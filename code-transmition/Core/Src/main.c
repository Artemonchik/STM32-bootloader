/* USER CODE BEGIN Header */
/**
 ******************************************************************************
 * @file           : main.c
 * @brief          : Main program body
 ******************************************************************************
 * @attention
 *
 * <h2><center>&copy; Copyright (c) 2020 STMicroelectronics.
 * All rights reserved.</center></h2>
 *
 * This software component is licensed by ST under BSD 3-Clause license,
 * the "License"; You may not use this file except in compliance with the
 * License. You may obtain a copy of the License at:
 *                        opensource.org/licenses/BSD-3-Clause
 *
 ******************************************************************************
 */
#define STR 1
#define ERRORSTR 2
#define PROGRAM 3
#define REQUEST 4
#define PAGE_SIZE 2048
#define CBC 1
#define AES256 1
#define BUF_SIZE (16*16)
#define RECEIVE_TRIES 5
//#define HAL_FLASH_MODULE_ENABLED
//#define FLASH_BASE            0x08000000UL /*!< FLASH base address in the alias region */
//#define CCMDATARAM_BASE       0x10000000UL /*!< CCM(core coupled memory) data RAM base address in the alias region     */
//#define SRAM_BASE             0x20000000UL /*!< SRAM base address in the alias region */
//#define PERIPH_BASE           0x40000000UL /*!< Peripheral base address in the alias region */
//#define SRAM_BB_BASE          0x22000000UL /*!< SRAM base address in the bit-band region */
//#define PERIPH_BB_BASE        0x42000000UL /*!< Peripheral base address in the bit-band region */
/* USER CODE END Header */
/* Includes ------------------------------------------------------------------*/
#include "main.h"

/* Private includes ----------------------------------------------------------*/
/* USER CODE BEGIN Includes */
#include "stdio.h"
#include "aes.h"
//#include "stm32f3xx_hal_flash.h"
//#include "stm32f3xx_hal_flash_ex.h"
#include "stdarg.h"
#include "string.h"
/* USER CODE END Includes */

/* Private typedef -----------------------------------------------------------*/
/* USER CODE BEGIN PTD */

/* USER CODE END PTD */

/* Private define ------------------------------------------------------------*/
/* USER CODE BEGIN PD */
/* USER CODE END PD */

/* Private macro -------------------------------------------------------------*/
/* USER CODE BEGIN PM */

/* USER CODE END PM */

/* Private variables ---------------------------------------------------------*/
UART_HandleTypeDef huart1;

/* USER CODE BEGIN PV */

/* USER CODE END PV */

/* Private function prototypes -----------------------------------------------*/
void SystemClock_Config(void);
static void MX_GPIO_Init(void);
static void MX_USART1_UART_Init(void);
/* USER CODE BEGIN PFP */

/* USER CODE END PFP */

/* Private user code ---------------------------------------------------------*/
/* USER CODE BEGIN 0 */
/**
 * integer length value of data you want to send in special format
 */

int need_to_change_baud = 0;
void check_baud(){
	if(need_to_change_baud){
		need_to_change_baud = 0;
		HAL_UART_DeInit(&huart1);
		if(huart1.Init.BaudRate == 3)
			return;
		huart1.Init.BaudRate = 2400;
		if (HAL_UART_Init(&huart1) != HAL_OK)
		{
		   Error_Handler();
		}
		HAL_Delay(100);
	}
}
HAL_StatusTypeDef sendData(UART_HandleTypeDef *huart, uint32_t messageCode,
		uint8_t *data, uint32_t len, uint32_t timeout) {
	check_baud();
	HAL_UART_Transmit(huart, (uint8_t*) (&messageCode), sizeof(messageCode), timeout);
	HAL_StatusTypeDef result = HAL_UART_Transmit(huart, (uint8_t*) (&len), sizeof(len), timeout);
	if(len != 0 && data != NULL)
		result = HAL_UART_Transmit(huart, data, len, timeout);
	return result;
}

/**
 * As you can see, max size of sending string after formating must be at most 255 characters*/
HAL_StatusTypeDef HAL_printf(const char *format, ...) {
	char buff[256];
	va_list arg;
	va_start(arg, format);
	vsprintf(buff, format, arg);
	HAL_StatusTypeDef result = sendData(&huart1, STR, (uint8_t*) buff,
			(int32_t) strlen(buff), 3000);
	va_end(arg);
	return result;
}
HAL_StatusTypeDef HAL_eprintf(const char *format, ...) {
	char buff[256];
	va_list arg;
	va_start(arg, format);
	vsprintf(buff, format, arg);
	HAL_StatusTypeDef result = sendData(&huart1, ERRORSTR, (uint8_t*) buff,
			(int) strlen(buff), 3000);
	va_end(arg);
	return result;
}

/**
 * @return uint_32 from UART or -1 if occurred and error
 */
int32_t receive_uint_32(UART_HandleTypeDef *huart, uint32_t timeout) {
	check_baud();
	int32_t result = -1;
	HAL_UART_Receive(huart, (uint8_t*) (&result), sizeof(int32_t), timeout);
	return result;
}

HAL_StatusTypeDef receiveBlock(UART_HandleTypeDef *huart, uint8_t *buff,
		uint32_t timeout, uint16_t buff_size) {
	check_baud();
	return HAL_UART_Receive(huart, buff,  buff_size, timeout);
}
void startSession(UART_HandleTypeDef *huart) {
	uint32_t code = 0xAE;
	sendData(huart, code, NULL, 0, 100);
}

/**
 * @note Do not forget unlock memory and erase pages where you want to store data
 */
HAL_StatusTypeDef storeBlock(uint8_t *buff, uint32_t address) {
	HAL_StatusTypeDef result = HAL_OK;
	for (int i = 0; i < BUF_SIZE; i += 4) {
		HAL_StatusTypeDef currResult = HAL_FLASH_Program(FLASH_TYPEPROGRAM_WORD,
				address + i, ((uint32_t*) buff)[i / 4]);
		if (currResult != HAL_OK) {
			result = currResult;
		}
	}
	return result;
}

/**
 * @param address contains address where we store the main program
 */
void bootloader_jump_to_user_app(uint32_t address) {

	typedef void (*pFunction)(void);

	pFunction JumpToApplication;

	uint32_t jumpAddress;

	// Initialize the user application Stack Pointer

	__set_MSP(*(__IO uint32_t*) address);

	// Jump to the user application

	// The stack pointer lives at APPLICATION_ADDRESS

	// The reset vector is at APPLICATION_ADDRESS + 4

	jumpAddress = *(__IO uint32_t*) (address + 4);

	JumpToApplication = (pFunction) jumpAddress;
// TODO: Make correct vector table init
	JumpToApplication();
}

/**
 * @note Do not forget unlock memory before cleaning any data
 */
HAL_StatusTypeDef preparePages(uint32_t address, uint32_t len) {
	uint32_t numberOfPages = (len / PAGE_SIZE) + 1;
	FLASH_EraseInitTypeDef eraseConfig = { FLASH_TYPEERASE_PAGES, address,
			numberOfPages };
	uint32_t PageError;
	return HAL_FLASHEx_Erase(&eraseConfig, &PageError);
}

void askForNextBlock(UART_HandleTypeDef *huart, uint32_t block_num) {
	sendData(huart, REQUEST, (uint8_t*)&block_num, sizeof(block_num), 1000);
}
uint8_t key[33] = "11111111111111111111111111111111";
struct AES_ctx ctx;

void decrypt(uint8_t * buff, size_t length){
	AES_CBC_decrypt_buffer(&ctx, buff, length);
}

uint32_t crc32(const char *s,size_t n) {
	uint32_t crc=0xFFFFFFFF;

	for(size_t i=0;i<n;i++) {
		char ch=s[i];
		for(size_t j=0;j<8;j++) {
			uint32_t b=(ch^crc)&1;
			crc>>=1;
			if(b) crc=crc^0xEDB88320;
			ch>>=1;
		}
	}

	return ~crc;
}
/* USER CODE END 0 */

/**
  * @brief  The application entry point.
  * @retval int
  */
int main(void)
{
  /* USER CODE BEGIN 1 */
	AES_init_ctx_iv(&ctx, key, key);
  /* USER CODE END 1 */

  /* MCU Configuration--------------------------------------------------------*/

  /* Reset of all peripherals, Initializes the Flash interface and the Systick. */
  HAL_Init();

  /* USER CODE BEGIN Init */

  /* USER CODE END Init */

  /* Configure the system clock */
  SystemClock_Config();

  /* USER CODE BEGIN SysInit */

  /* USER CODE END SysInit */

  /* Initialize all configured peripherals */
  MX_GPIO_Init();
  MX_USART1_UART_Init();
  /* USER CODE BEGIN 2 */
	uint32_t address = 0x08020000;
	uint32_t timeout = 500;
	startSession(&huart1);
	int32_t dataCode = receive_uint_32(&huart1, timeout);
	int32_t len = receive_uint_32(&huart1, timeout);
	HAL_printf("%d - bytes going to be received", len);
	HAL_printf("%u - data code was received", dataCode);
	if (len == -1) {
		bootloader_jump_to_user_app(address);
		HAL_printf("No data was received, starts the main program");
	}
	if (len % BUF_SIZE != 0) {
		HAL_eprintf("Length of the file must be divisible by BUF_SIZE");
		return 2;
	}

	if (dataCode == PROGRAM) {
		HAL_printf("Program is pending");
		if (HAL_FLASH_Unlock() == HAL_OK) {
			HAL_printf("Unlocking was successful");
		} else {
			HAL_eprintf("Unlocking failed");
		};
		HAL_StatusTypeDef result = preparePages(address, len);
		if (result != HAL_OK) {
			HAL_eprintf(
					"An error occurred while erasing pages started with the address",
					address);
			return 2;
		}
		if (result == HAL_OK) {
			HAL_printf("Pages was erased successfully");
		}

		int unsuccessful_receive_attempts = 0;
		for (int32_t i = 0; i < len; i += BUF_SIZE, address += BUF_SIZE) {
			uint8_t buff[BUF_SIZE];
			askForNextBlock(&huart1, i);
			HAL_StatusTypeDef result = receiveBlock(&huart1, buff,
					timeout + 400, BUF_SIZE);
			uint32_t crc_received;
			receiveBlock(&huart1, (uint8_t*)&crc_received, timeout + 400, sizeof(crc_received));
			HAL_printf("Value crc received = %u", crc_received);
			uint32_t crc_computed = crc32((char*)buff, BUF_SIZE);
			HAL_printf("Value crc computed = %u", crc_computed);
			if(crc_computed != crc_received){
				unsuccessful_receive_attempts++;
				if(unsuccessful_receive_attempts == RECEIVE_TRIES){
					HAL_printf("Decrease baudrate");
					need_to_change_baud =  1;
				}
				HAL_eprintf("do not match, require re-sending");
				i -= BUF_SIZE;
				address -= BUF_SIZE;
				continue;
			}else{
				unsuccessful_receive_attempts = 0;
			}
			decrypt(buff, BUF_SIZE);
			if (result != HAL_OK) {
				HAL_eprintf(
						"An error occurred while transferring data: %d block",
						i / BUF_SIZE);
				return 2;
			}
			if (result == HAL_OK) {
				HAL_printf("%d block was received", i / BUF_SIZE);
			}

			HAL_StatusTypeDef writeResult = storeBlock(buff, address);
			if (writeResult == HAL_OK) {
				HAL_printf("%d block was received and stored at 0x%x address",
						i / BUF_SIZE, address);
			} else {
				HAL_printf(
						"An error occurred while writing data: %d block in 0x%x address",
						i / BUF_SIZE, address);
			}

		}
		HAL_FLASH_Lock();
	}

	HAL_printf(
			"#####\n#####\n All data was received and successfully stored \n#####\n#####\n");
	address = 0x08020000;
	bootloader_jump_to_user_app(address);
  /* USER CODE END 2 */

  /* Infinite loop */
  /* USER CODE BEGIN WHILE */

	while (1) {
    /* USER CODE END WHILE */

    /* USER CODE BEGIN 3 */
	}

  /* USER CODE END 3 */
}

/**
  * @brief System Clock Configuration
  * @retval None
  */
void SystemClock_Config(void)
{
  RCC_OscInitTypeDef RCC_OscInitStruct = {0};
  RCC_ClkInitTypeDef RCC_ClkInitStruct = {0};
  RCC_PeriphCLKInitTypeDef PeriphClkInit = {0};

  /** Initializes the RCC Oscillators according to the specified parameters
  * in the RCC_OscInitTypeDef structure.
  */
  RCC_OscInitStruct.OscillatorType = RCC_OSCILLATORTYPE_HSI;
  RCC_OscInitStruct.HSIState = RCC_HSI_ON;
  RCC_OscInitStruct.HSICalibrationValue = RCC_HSICALIBRATION_DEFAULT;
  RCC_OscInitStruct.PLL.PLLState = RCC_PLL_NONE;
  if (HAL_RCC_OscConfig(&RCC_OscInitStruct) != HAL_OK)
  {
    Error_Handler();
  }
  /** Initializes the CPU, AHB and APB buses clocks
  */
  RCC_ClkInitStruct.ClockType = RCC_CLOCKTYPE_HCLK|RCC_CLOCKTYPE_SYSCLK
                              |RCC_CLOCKTYPE_PCLK1|RCC_CLOCKTYPE_PCLK2;
  RCC_ClkInitStruct.SYSCLKSource = RCC_SYSCLKSOURCE_HSI;
  RCC_ClkInitStruct.AHBCLKDivider = RCC_SYSCLK_DIV1;
  RCC_ClkInitStruct.APB1CLKDivider = RCC_HCLK_DIV1;
  RCC_ClkInitStruct.APB2CLKDivider = RCC_HCLK_DIV1;

  if (HAL_RCC_ClockConfig(&RCC_ClkInitStruct, FLASH_LATENCY_0) != HAL_OK)
  {
    Error_Handler();
  }
  PeriphClkInit.PeriphClockSelection = RCC_PERIPHCLK_USART1;
  PeriphClkInit.Usart1ClockSelection = RCC_USART1CLKSOURCE_PCLK2;
  if (HAL_RCCEx_PeriphCLKConfig(&PeriphClkInit) != HAL_OK)
  {
    Error_Handler();
  }
}

/**
  * @brief USART1 Initialization Function
  * @param None
  * @retval None
  */
static void MX_USART1_UART_Init(void)
{

  /* USER CODE BEGIN USART1_Init 0 */

  /* USER CODE END USART1_Init 0 */

  /* USER CODE BEGIN USART1_Init 1 */

  /* USER CODE END USART1_Init 1 */
  huart1.Instance = USART1;
  huart1.Init.BaudRate = 115200;
  huart1.Init.WordLength = UART_WORDLENGTH_8B;
  huart1.Init.StopBits = UART_STOPBITS_1;
  huart1.Init.Parity = UART_PARITY_NONE;
  huart1.Init.Mode = UART_MODE_TX_RX;
  huart1.Init.HwFlowCtl = UART_HWCONTROL_NONE;
  huart1.Init.OverSampling = UART_OVERSAMPLING_16;
  huart1.Init.OneBitSampling = UART_ONE_BIT_SAMPLE_DISABLE;
  huart1.AdvancedInit.AdvFeatureInit = UART_ADVFEATURE_NO_INIT;
  if (HAL_UART_Init(&huart1) != HAL_OK)
  {
    Error_Handler();
  }
  /* USER CODE BEGIN USART1_Init 2 */

  /* USER CODE END USART1_Init 2 */

}

/**
  * @brief GPIO Initialization Function
  * @param None
  * @retval None
  */
static void MX_GPIO_Init(void)
{

  /* GPIO Ports Clock Enable */
  __HAL_RCC_GPIOA_CLK_ENABLE();

}

/* USER CODE BEGIN 4 */

/* USER CODE END 4 */

/**
  * @brief  This function is executed in case of error occurrence.
  * @retval None
  */
void Error_Handler(void)
{
  /* USER CODE BEGIN Error_Handler_Debug */
	/* User can add his own implementation to report the HAL error return state */

  /* USER CODE END Error_Handler_Debug */
}

#ifdef  USE_FULL_ASSERT
/**
  * @brief  Reports the name of the source file and the source line number
  *         where the assert_param error has occurred.
  * @param  file: pointer to the source file name
  * @param  line: assert_param error line source number
  * @retval None
  */
void assert_failed(uint8_t *file, uint32_t line)
{
  /* USER CODE BEGIN 6 */
  /* User can add his own implementation to report the file name and line number,
     tex: printf("Wrong parameters value: file %s on line %d\r\n", file, line) */
  /* USER CODE END 6 */
}
#endif /* USE_FULL_ASSERT */

/************************ (C) COPYRIGHT STMicroelectronics *****END OF FILE****/
