#include "meta_info.h"
#include <stdint.h>
#include "string.h"
#include "encryption.h"
void readMetaInfo(BootloaderMetaInfo * info, void * address){
	memcpy((void *) info, address, sizeof(BootloaderMetaInfo));
}

void readKey(uint8_t * buff, void * address){
	memcpy((void *) buff, address, KEY_SIZE);
}

