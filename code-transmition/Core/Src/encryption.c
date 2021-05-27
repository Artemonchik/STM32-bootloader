#include "checksum.h"
#include "encryption.h"
void decrypt(struct AES_ctx * ctx, uint8_t * buff, size_t length){
	AES_CBC_decrypt_buffer(ctx, buff, length);
}

void encrypt(struct AES_ctx * ctx, uint8_t * buff, size_t length){
	AES_CBC_encrypt_buffer(ctx, buff, length);
}
