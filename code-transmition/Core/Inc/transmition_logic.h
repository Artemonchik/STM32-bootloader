#ifndef INC_TRANSMITION_LOGIC_H_
#define INC_TRANSMITION_LOGIC_H_

#include <stdint.h>
#include <stdio.h>
#include <stdarg.h>
#include "checksum.h"
#include "main.h"
#include "status.h"
#define STR 1
#define ERRORSTR 2
#define PROGRAM 3
#define REQUEST 4
#define ACK 5
#define NEXT 6
#define BAUDRATE 7
#define TIMEOUT 8
#define RELEASE 9
#define SECRET_KEY 10
#define BUF_SIZE (16*16)
#define START_SESSION 0xAE

#pragma pack(1)
typedef struct Pair_s{
	uint32_t from;
	uint32_t to;
	uint32_t crc;
} Pair;

#pragma pack(1)
typedef struct HeaderPack_s{
	uint32_t messageCode;
	uint32_t len;
	uint32_t num;
	uint32_t crc;
} HeaderPack;


uint32_t computeHeaderCrc(HeaderPack * header);
void makeHeader(HeaderPack * header, uint32_t messageCode, uint32_t len);
Status verifyDataHeader(HeaderPack * header);
Status sendDataHeader( HeaderPack * header, uint32_t timeout);
Status sendFullPacket( HeaderPack * header, uint8_t *data, uint32_t timeout);
Status receiveDataHeader(HeaderPack * header, uint32_t timeout);
Status receiveFullPacket(uint8_t * buff, HeaderPack * header, uint32_t * crc,HeaderPack * outputHeader, uint32_t timeout);
Status sendData(HeaderPack * header,
		uint8_t *data, uint32_t timeout);
Status sendAck(uint32_t num, uint32_t timeout);
Status receiveData(uint8_t * buff, HeaderPack * header, uint32_t timeout);


void askForNextBlock(uint32_t from, uint32_t to, uint32_t timeout);
void sendReadyToNextCommand(uint32_t timeout);
Status HAL_printf(const char *format, ...);
Status startSession();
#endif
