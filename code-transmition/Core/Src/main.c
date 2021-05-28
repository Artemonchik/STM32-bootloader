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

#if defined(DEBUG)
	#define ADDRESS 0x08006080
#else
	#define ADDRESS 0x08004080
#endif

#define META_INFO_ADDRESS (ADDRESS - 0x80)
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
#include "stdarg.h"
#include "transmition_logic.h"
#include "encryption.h"
#include "checksum.h"
#include "periphery.h"
#include "meta_info.h"
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

uint8_t key[32] = {0};
extern unsigned int symbol_1;
BootloaderMetaInfo info = {0};
struct AES_ctx ctx;
void loop(){
	while(1);
}
/* USER CODE END 0 */

/**
  * @brief  The application entry point.
  * @retval int
  */
int main(void)
{
  /* USER CODE BEGIN 1 */
	readMetaInfo(&info, (void *) META_INFO_ADDRESS);
	readKey(key, &symbol_1);
	AES_init_ctx_iv(&ctx, key, info.info.iv);

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
		if(!validate_program((uint8_t *) ADDRESS, info.info.size, info.program_crc)){
			loop();
		}
		bootloader_jump_to_user_app(address);
	}

	while(1){
		uint8_t buff[3 * BUF_SIZE + CRC_SIZE];
		HeaderPack header;
		sendReadyToNextCommand(timeout);
		receiveData(buff, &header, 36000000);
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
		if(header.messageCode == FIRMWARE_INFO){
			memcpy((void *)&info, (void*)&buff[0], sizeof(Firmware_info));
			AES_init_ctx_iv(&ctx, key, info.info.iv);
			HAL_printf("OK, iv is: %s", info.info.iv);
		}if(header.messageCode == FIRMWARE_INFO_UPLOAD){
			makeHeader(&header, FIRMWARE_INFO_UPLOAD, sizeof(info.info));
			memcpy(buff, (void *)&info.info, sizeof(info.info));
			sendData(&header, buff, timeout);
		}if(header.messageCode == UPLOAD_CODE || header.messageCode == UPLOAD_DATA){
			int isCode = header.messageCode == UPLOAD_CODE;
			int code = isCode ? UPLOAD_CODE : UPLOAD_DATA;
			struct AES_ctx upload_ctx;
			AES_init_ctx_iv(&upload_ctx, key, info.info.iv);
			if(isCode){
				makeHeader(&header, code, sizeof(uint32_t) * 4);
				memcpy(buff, (void *)&info.addresses, sizeof(uint32_t) * 4);
				encrypt(&upload_ctx, buff, sizeof(uint32_t) * 4);
				sendData(&header, buff, timeout);
			}
			uint32_t startAddress = isCode ? ADDRESS : info.addresses.from;
			uint32_t endAddress = isCode ? (ADDRESS + info.info.size) : info.addresses.to;
			for(int i = 0; i < endAddress - startAddress; i += BUF_SIZE){
				code = isCode ? PROGRAM : DATA;
				int from = i;
				int to = MIN(i + BUF_SIZE, endAddress - startAddress);
				makeHeader(&header, PROGRAM, to - from);
				memcpy(buff, (void *) (startAddress + i), to - from);
				encrypt(&upload_ctx, buff, to - from);
				sendData(&header, buff, timeout);
			}
			makeHeader(&header, END_OF_UPLOAD, 0);
			sendData(&header, buff, timeout);
		}
		if(header.messageCode == ADDRESSES_INFO){
			decrypt(&ctx, buff, sizeof(uint32_t) * 4);
			memcpy((void *)&info.addresses, buff, sizeof(Addresses));
			memcpy((void *)&info.program_crc, buff + sizeof(Addresses), sizeof(uint32_t));
			if(crc32((char *)&info.addresses.from, sizeof(info.addresses.from) + sizeof(info.addresses.to)) != info.addresses.addresses_crc){
				HAL_printf("Crc for addresses is not correct %ux %ux", info.addresses.from, info.addresses.to);
				loop();
			}
			HAL_printf("U said that i must download addresses from %x %x", info.addresses.from, info.addresses.to);
		}
		if (header.messageCode == PROGRAM) {
			uint32_t len = *(uint32_t*)buff;
			HAL_printf("Program is pending with len %d", len);
			if (HAL_FLASH_Unlock() == HAL_OK) {
				HAL_printf("Unlocking was successful");
			} else {
				HAL_printf("Unlocking failed");
			};
			Status result = erasePages(address, len);
			if (result != STATUS_OK) {
				HAL_printf(
						"An error occurred while erasing pages started with the address",
						address);
			}else {
				HAL_printf("Pages was erased successfully");
			}
			storeBlock((void *)&info, sizeof(BootloaderMetaInfo),  META_INFO_ADDRESS);
			for (uint32_t i = 0; i < len; i += BUF_SIZE) {
				int from = i;
				int to = MIN(i + BUF_SIZE, len);
				askForNextBlock( from, to, timeout);
				Status result = receiveData(buff, &header, timeout);
				decrypt(&ctx, (uint8_t*)buff, to - from);
				if (result != STATUS_OK) {
					HAL_printf(
							"An error occurred while transferring data: %d block",
							i / BUF_SIZE);
					return 2;
				}
				if (result == STATUS_OK) {
					HAL_printf("%d block was received %x %x", i / BUF_SIZE, from, to);
				}

				Status writeResult = storeBlock(buff, to - from, address + i);
				if (writeResult == STATUS_OK) {
					HAL_printf("%d block was received and stored at 0x%x address",
							i / BUF_SIZE, address + i);
				} else {
					HAL_printf(
							"An error occurred while writing data: %d block in 0x%x address",
							i / BUF_SIZE, address + i);
				}

			}
			if(validate_program((uint8_t *) ADDRESS, info.info.size, info.program_crc)){
				HAL_printf("Program crc is correct. " );
			}else{
				HAL_printf("Program crc is incorrect size: %d, expected %d", info.info.size, info.program_crc);
				loop();
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
