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


#define BUF_SIZE (16*16)
#define ADDRESS 0x08004080

#define MIN(X, Y) (((X) < (Y)) ? (X) : (Y))
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
#include "string.h"
//#include "stm32f3xx_hal_flash.h"
//#include "stm32f3xx_hal_flash_ex.h"
#include "stdarg.h"
#include "transmition_logic.h"
#include "encryption.h"
#include "checksum.h"
#include "periphery.h"
//#include "string.h"
/* USER CODE END Includes */

/* Private typedef -----------------------------------------------------------*/
/* USER CODE BEGIN PTD */
#pragma pack(1)
typedef struct FirmwareVersion_s{
	char companyName[10];
	uint16_t version[4];
	uint64_t date;
	uint32_t size;
} FirmwareVersion;



#pragma pack(1)
typedef struct BootloaderInfo_s{
	uint8_t key[32];
} BootloaderInfo;
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

HAL_StatusTypeDef storeBlock(uint8_t *buff, int n, uint32_t address) {
	HAL_StatusTypeDef result = HAL_OK;
	for (int i = 0; i < n; i += 4) {
		HAL_StatusTypeDef currResult = HAL_FLASH_Program(FLASH_TYPEPROGRAM_WORD,
				address + i, ((uint32_t*) buff)[i / 4]);
		if (currResult != HAL_OK) {
			result = currResult;
			return result;
		}
	}
	return result;
}

/**
 * @param address contains address where we store the main program
 */


/**
 * @note Do not forget unlock memory before cleaning any data
 */




uint8_t key[32] = {0};
uint8_t iv[16] = {0};
extern unsigned int symbol_1;

/* USER CODE END 0 */

/**
  * @brief  The application entry point.
  * @retval int
  */
int main(void)
{
  /* USER CODE BEGIN 1 */
	memcpy((void *) &key[0], (void *) &symbol_1, sizeof(uint8_t) * 32);
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
	uint32_t address = ADDRESS;
	uint32_t timeout = 3600;
	HAL_StatusTypeDef res = startSession();
	if (res != HAL_OK) {
		bootloader_jump_to_user_app(address);
	}

	while(1){
		uint8_t buff[2 * BUF_SIZE + CRC_SIZE];
		HeaderPack header;
		sendReadyToNextCommand(timeout);
		receiveData( buff, &header, 3600000);
		if(header.messageCode == BAUDRATE){
				uint32_t baudrate = *(uint32_t*)buff;
				changeSpeed( baudrate);
				continue;
		}
		if(header.messageCode == TIMEOUT){
			uint32_t t = *(uint32_t*)buff;
			timeout = t;
		}
		if(header.messageCode == RELEASE){
			bootloader_jump_to_user_app(address);
			break;
		}
		if(header.messageCode == SECRET_KEY){
			memcpy(buff, key, KEY_SIZE);
			makeHeader(&header, SECRET_KEY, KEY_SIZE);
			sendData( &header, buff, timeout);
		}
		if (header.messageCode == PROGRAM) {
			struct AES_ctx ctx;
			memcpy((void *) &iv[0], (void *)& symbol_1, sizeof(uint8_t) * 16);
			AES_init_ctx_iv(&ctx, key, iv);
			uint32_t len = *(uint32_t*)buff;
			HAL_printf("Program is pending with len %d", len);
			if (HAL_FLASH_Unlock() == HAL_OK) {
				HAL_printf("Unlocking was successful");
			} else {
				HAL_printf("Unlocking failed");
			};
			HAL_StatusTypeDef result = erasePages(address, len);
			if (result != HAL_OK) {
				HAL_printf(
						"An error occurred while erasing pages started with the address",
						address);
			}else {
				HAL_printf("Pages was erased successfully");
			}
			for (uint32_t i = 0; i < len; i += BUF_SIZE) {
				int from = i;
				int to = MIN(i + BUF_SIZE, len);
				askForNextBlock( from, to, timeout);
				HAL_StatusTypeDef result = receiveData( buff, &header, timeout);
				int c = 0;
				c++;
				decrypt(&ctx, (uint8_t*)buff, to - from);
				if (result != HAL_OK) {
					HAL_printf(
							"An error occurred while transferring data: %d block",
							i / BUF_SIZE);
					return 2;
				}
				if (result == HAL_OK) {
					HAL_printf("%d block was received", i / BUF_SIZE);
				}

				HAL_StatusTypeDef writeResult = storeBlock(buff, to - from, address + i);
				if (writeResult == HAL_OK) {
					HAL_printf("%d block was received and stored at 0x%x address",
							i / BUF_SIZE, address + i);
				} else {
					HAL_printf(
							"An error occurred while writing data: %d block in 0x%x address",
							i / BUF_SIZE, address + i);
				}

			}
			HAL_FLASH_Lock();
			}
		}

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
  huart1.AdvancedInit.AdvFeatureInit = UART_ADVFEATURE_RXOVERRUNDISABLE_INIT;
  huart1.AdvancedInit.OverrunDisable = UART_ADVFEATURE_OVERRUN_DISABLE;
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
