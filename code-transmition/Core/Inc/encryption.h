#ifndef INC_ENCRYPTION_H_
#define INC_ENCRYPTION_H_

#include <stdint.h>
#include <stdio.h>
#define KEY_SIZE 32
#define AES256 1
#define CBC 1
#include "aes.h"

void decrypt(struct AES_ctx * ctx, uint8_t * buff, size_t length);
void encrypt(struct AES_ctx * ctx, uint8_t * buff, size_t length);
void generateIV(uint8_t * iv, uint8_t * address);

#endif
