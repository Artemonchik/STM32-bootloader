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
#define PAGE_SIZE 2048
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
HAL_StatusTypeDef sendData(UART_HandleTypeDef *huart, int32_t messageCode, uint8_t *data,
		uint32_t len, uint32_t timeout) {
	HAL_UART_Transmit(huart, (uint8_t*) (&len), sizeof(len), timeout);
	HAL_UART_Transmit(huart, (uint8_t*) (&messageCode), sizeof(messageCode),
			timeout);
	return HAL_UART_Transmit(huart, data, len, timeout);
}

/**
 * As you can see, max size of sending string after formating must be at most 255 characters*/
HAL_StatusTypeDef HAL_printf(const char *format, ...) {
	char buff[256];
	va_list arg;
	va_start(arg, format);
	vsprintf(buff, format, arg);
	HAL_StatusTypeDef result = sendData(&huart1, STR, (uint8_t*) buff, (int32_t) strlen(buff), 3000);
	va_end(arg);
	return result;
}
HAL_StatusTypeDef HAL_eprintf(const char *format, ...) {
	char buff[256];
	va_list arg;
	va_start(arg, format);
	vsprintf(buff, format, arg);
	HAL_StatusTypeDef result = sendData(&huart1, ERRORSTR, (uint8_t*) buff, (int) strlen(buff), 3000);
	va_end(arg);
	return result;
}

uint32_t receive_uint_32(UART_HandleTypeDef *huart, uint32_t timeout){
	uint32_t result = 3;
	HAL_UART_Receive(huart, (uint8_t*)&result, sizeof(uint32_t), timeout);
	return result;
}

HAL_StatusTypeDef receive128bit(UART_HandleTypeDef *huart, uint8_t * buff, uint32_t timeout){
	return HAL_UART_Receive(huart, buff, 16, timeout);
}
void sendStartCode(UART_HandleTypeDef *huart){
	uint8_t code = 0xAE;
	HAL_UART_Transmit(huart, &code, sizeof(uint8_t), 100);
}

/**
 * @note Do not forget unlock memory and erase pages where you want to store data
 */
HAL_StatusTypeDef store128bit(uint8_t * buff, uint32_t address){
	HAL_StatusTypeDef result = HAL_OK;
	for(int i = 0; i < 16; i+= 4){
		HAL_StatusTypeDef currResult = HAL_FLASH_Program(FLASH_TYPEPROGRAM_WORD,
				address + i, ((uint32_t*) buff)[i]);
		if(currResult != HAL_OK){
			result = currResult;
		}
	}
	return result;
};
/**
 * @note Do not forget unlock memory before cleaning any data
 */
HAL_StatusTypeDef preparePages(uint32_t address, uint32_t len){
	uint32_t numberOfPages = (len / PAGE_SIZE) + 1;
	FLASH_EraseInitTypeDef eraseConfig = { FLASH_TYPEERASE_PAGES, address, numberOfPages};
	uint32_t PageError;
	return HAL_FLASHEx_Erase(&eraseConfig, &PageError);
}
/* USER CODE END 0 */

/**
 * @brief  The application entry point.
 * @retval int
 */

/**
 * Now we are going to send data by UART and write the program on the flash
 * First we send 0xABCDEF byte by UART to say our desktop application that we want to receive data
 * Second we receive int32_t variable (4 bytes) to find out how many program store next step is to write it on flash
 * After success receive we

 */
int main(void) {
	/* USER CODE BEGIN 1 */

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
	uint32_t timeout = 3000;
	sendStartCode(&huart1);

	uint32_t len = receive_uint_32(&huart1, timeout);
	if (len % 16 != 0) {
		HAL_eprintf("Length of the file must be divisible by 16\n");
		return 2;
	}

	uint32_t dataCode = receive_uint_32(&huart1, timeout);
	if (dataCode == PROGRAM) {
		uint32_t address = 0x80020000;
		HAL_FLASH_Unlock();
		HAL_StatusTypeDef result = preparePages(address, len);
		if (result != HAL_OK) {
			HAL_eprintf(
					"An error occurred while erasing pages started with the address\n",
					address);
			return 2;
		}
		if (result == HAL_OK) {
			HAL_printf("Pages was erased successfully");
		}

		for (int32_t i = 0; i < len; i += 16, address += 16) {
			uint8_t buff[16];
			HAL_StatusTypeDef result = receive128bit(&huart1, buff, timeout);
			if (result != HAL_OK) {
				HAL_eprintf(
						"An error occurred while transferring data: %d block\n",
						i / 16);
				return 2;
			}
			if (result == HAL_OK) {
				HAL_printf("%d block was received\n", i / 16);
			}

			HAL_StatusTypeDef writeResult = store128bit(buff, address);
			if (writeResult != HAL_OK) {
				HAL_eprintf(
						"An error occurred while writing data: %d block in 0x%x address\n",
						i / 16, address);
				return 2;
			}
			if (writeResult == HAL_OK) {
				HAL_printf("%d block was received and stored at 0x%x address\n", i / 16, address);
			}
		}
		HAL_FLASH_Lock();
	}

	HAL_printf("#####\n#####\n All data was received and successfully stored \n#####\n#####\n");
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
void SystemClock_Config(void) {
	RCC_OscInitTypeDef RCC_OscInitStruct = { 0 };
	RCC_ClkInitTypeDef RCC_ClkInitStruct = { 0 };
	RCC_PeriphCLKInitTypeDef PeriphClkInit = { 0 };

	/** Initializes the RCC Oscillators according to the specified parameters
	 * in the RCC_OscInitTypeDef structure.
	 */
	RCC_OscInitStruct.OscillatorType = RCC_OSCILLATORTYPE_HSI;
	RCC_OscInitStruct.HSIState = RCC_HSI_ON;
	RCC_OscInitStruct.HSICalibrationValue = RCC_HSICALIBRATION_DEFAULT;
	RCC_OscInitStruct.PLL.PLLState = RCC_PLL_NONE;
	if (HAL_RCC_OscConfig(&RCC_OscInitStruct) != HAL_OK) {
		Error_Handler();
	}
	/** Initializes the CPU, AHB and APB buses clocks
	 */
	RCC_ClkInitStruct.ClockType = RCC_CLOCKTYPE_HCLK | RCC_CLOCKTYPE_SYSCLK
			| RCC_CLOCKTYPE_PCLK1 | RCC_CLOCKTYPE_PCLK2;
	RCC_ClkInitStruct.SYSCLKSource = RCC_SYSCLKSOURCE_HSI;
	RCC_ClkInitStruct.AHBCLKDivider = RCC_SYSCLK_DIV1;
	RCC_ClkInitStruct.APB1CLKDivider = RCC_HCLK_DIV1;
	RCC_ClkInitStruct.APB2CLKDivider = RCC_HCLK_DIV1;

	if (HAL_RCC_ClockConfig(&RCC_ClkInitStruct, FLASH_LATENCY_0) != HAL_OK) {
		Error_Handler();
	}
	PeriphClkInit.PeriphClockSelection = RCC_PERIPHCLK_USART1;
	PeriphClkInit.Usart1ClockSelection = RCC_USART1CLKSOURCE_PCLK2;
	if (HAL_RCCEx_PeriphCLKConfig(&PeriphClkInit) != HAL_OK) {
		Error_Handler();
	}
}

/**
 * @brief USART1 Initialization Function
 * @param None
 * @retval None
 */
static void MX_USART1_UART_Init(void) {

	/* USER CODE BEGIN USART1_Init 0 */

	/* USER CODE END USART1_Init 0 */

	/* USER CODE BEGIN USART1_Init 1 */

	/* USER CODE END USART1_Init 1 */
	huart1.Instance = USART1;
	huart1.Init.BaudRate = 9600;
	huart1.Init.WordLength = UART_WORDLENGTH_8B;
	huart1.Init.StopBits = UART_STOPBITS_1;
	huart1.Init.Parity = UART_PARITY_NONE;
	huart1.Init.Mode = UART_MODE_TX_RX;
	huart1.Init.HwFlowCtl = UART_HWCONTROL_NONE;
	huart1.Init.OverSampling = UART_OVERSAMPLING_16;
	huart1.Init.OneBitSampling = UART_ONE_BIT_SAMPLE_DISABLE;
	huart1.AdvancedInit.AdvFeatureInit = UART_ADVFEATURE_NO_INIT;
	if (HAL_UART_Init(&huart1) != HAL_OK) {
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
static void MX_GPIO_Init(void) {

	/* GPIO Ports Clock Enable */
	__HAL_RCC_GPIOA_CLK_ENABLE();

}

/* USER CODE BEGIN 4 */

/* USER CODE END 4 */

/**
 * @brief  This function is executed in case of error occurrence.
 * @retval None
 */
void Error_Handler(void) {
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
