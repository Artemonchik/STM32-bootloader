#ifndef INC_META_INFO_H_
#define INC_META_INFO_H_
#include <stdint.h>

#pragma pack(1)
typedef struct Firmware_info_s{
	uint8_t header[6];
	uint8_t name[32];
	uint8_t version[8];
	uint8_t creationTime[8];
	uint8_t DATA[4];
	uint8_t iv[16];
	uint8_t size[4];
	uint8_t padding[2];
} Firmware_info;

#pragma pack(1)
typedef struct Addresses_s{
	uint32_t from;
	uint32_t to;
} Addresses;

#pragma pack(1)
typedef struct BootloaderMetaInfo_s{
	Firmware_info info;
	Addresses addresses;
} BootloaderMetaInfo;

void readKey(uint8_t * buff, void * address);
void readMetaInfo(BootloaderMetaInfo * info, void * address);
#endif /* INC_META_INFO_H_ */
