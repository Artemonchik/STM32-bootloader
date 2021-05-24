#ifndef INC_PERIPHERY_H_
#define INC_PERIPHERY_H_

#include "status.h"
#define PAGE_SIZE FLASH_PAGE_SIZE

HAL_StatusTypeDef erasePages(uint32_t address, uint32_t len);
void changeSpeed(uint32_t speed);
void bootloader_jump_to_user_app(uint32_t address);
Status transmit(uint8_t * buff, size_t n, uint32_t timeout);
Status receive(uint8_t * buff, size_t n, uint32_t timeout);
Status storeBlock(uint8_t *buff, int n, uint32_t address);

#endif
