#include "main.h"
#include "periphery.h"
/*
 * Externs:
 */
extern UART_HandleTypeDef huart1;

// This function must erase enough storage for the data that begins at address and has len bytes
HAL_StatusTypeDef erasePages(uint32_t address, uint32_t len) {
	uint32_t numberOfPages = (len / PAGE_SIZE) + 1;
	FLASH_EraseInitTypeDef eraseConfig;
	eraseConfig.TypeErase = FLASH_TYPEERASE_PAGES;
	eraseConfig.NbPages = numberOfPages;
	eraseConfig.PageAddress = address;
	uint32_t PageError;
	return HAL_FLASHEx_Erase(&eraseConfig, &PageError);
}
/*
 * Change speed of the interface u are using
 */
void changeSpeed(uint32_t speed){
	HAL_UART_DeInit(&huart1);
	huart1.Init.BaudRate = speed;
	if (HAL_UART_Init(&huart1) != HAL_OK) {
	    Error_Handler();
	}
}
/*
 * Jump to the firmware program
 */
void bootloader_jump_to_user_app(uint32_t address) {
	typedef void (*pFunction)(void);
	pFunction JumpToApplication;
	uint32_t jumpAddress;
	__set_MSP(*(__IO uint32_t*) address);
	jumpAddress = *(__IO uint32_t*) (address + 4);
	JumpToApplication = (pFunction) jumpAddress;
	JumpToApplication();
}

Status __HAL_to_Status(HAL_StatusTypeDef res){
	if(res == HAL_OK){
		return STATUS_OK;
	} else if(res == HAL_ERROR){
		return STATUS_ERROR;
	} else if(res == HAL_TIMEOUT){
		return STATUS_TIMEOUT;
	}else{
		return STATUS_BUSY;
	}
}

Status transmit(uint8_t * buff, size_t n, uint32_t timeout){
	HAL_StatusTypeDef res = HAL_UART_Transmit(&huart1, buff, (size_t) n, timeout);
	return __HAL_to_Status(res);
}
Status receive(uint8_t * buff, size_t n, uint32_t timeout){
	HAL_StatusTypeDef res = HAL_UART_Receive(&huart1, buff, (size_t) n, timeout);
	return __HAL_to_Status(res);
}


