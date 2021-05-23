#include "checksum.h"
#include "encryption.h"
void decrypt(struct AES_ctx * ctx, uint8_t * buff, size_t length){
	AES_CBC_decrypt_buffer(ctx, buff, length);
}

void encrypt(struct AES_ctx * ctx, uint8_t * buff, size_t length){
	AES_CBC_encrypt_buffer(ctx, buff, length);
}

void generateIV(uint8_t * iv, uint8_t * address){
	uint32_t arr[4];

	for(int i = 0; i < 4; i++){
		arr[i] = crc32((char *) (address + i * 1000), 1000);
	}

	memcpy((char *)iv, (char *)arr, sizeof(arr));
}
