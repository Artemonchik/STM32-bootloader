#include "transmition_logic.h"
#include "string.h"
#include "periphery.h"

uint32_t packet_counter = 0;
uint32_t computeHeaderCrc(HeaderPack * header){
	return crc32((char*)header, sizeof(HeaderPack) - sizeof(header->crc));
}

void makeHeader(HeaderPack * header, uint32_t messageCode, uint32_t len){
	header->messageCode = messageCode;
	header->len = len;
//	header->num = num;
	header->crc = computeHeaderCrc(header);
}

Status verifyDataHeader(HeaderPack * header){
	if(computeHeaderCrc(header) != header->crc){
		return HAL_ERROR;
	}
	return HAL_OK;
}

Status sendDataHeader(HeaderPack * header, uint32_t timeout){
	header->crc = computeHeaderCrc(header);
	Status res = transmit( (uint8_t*) (header), sizeof(HeaderPack), timeout);
	return res;
}

Status sendDataBody(HeaderPack * header, uint8_t *data, uint32_t timeout) {
	if(header->len == 0 || data == NULL){
		return 0;
	}
	uint32_t crc = crc32((char*)data, header->len);
	memcpy((void *)(data + header->len), (void *)&crc, sizeof(crc));
	Status result = transmit(data, header->len + sizeof(crc), timeout);
	return result;
}


Status receiveDataHeader(HeaderPack * header, uint32_t timeout){
	Status res = receive((uint8_t *) header, (uint16_t) sizeof(HeaderPack), timeout);
	return res;
}

Status receiveDataBodyWithCRC(uint8_t * buff, HeaderPack * header, uint32_t * crc, uint32_t timeout){
	Status res = receive( buff, header->len + sizeof(uint32_t), timeout);
	*crc = *((uint32_t *)(&buff[header->len]));
	return res;
}

Status sendData(HeaderPack * header,
		uint8_t *data, uint32_t timeout){
	header->num = packet_counter++;
	Status result = STATUS_ERROR;

	while(result != STATUS_OK){
		result = sendDataHeader( header, timeout);
		if(result != STATUS_OK)
			continue;
		HeaderPack receivedHeader = {0};
		result = receiveDataHeader( &receivedHeader, timeout);
		if(result != STATUS_OK)
			continue;

		if(verifyDataHeader(&receivedHeader) != STATUS_OK ||
				receivedHeader.messageCode != ACK /*||
				receivedHeader.num != header->num + 1*/){
			result = STATUS_ERROR;
			continue;
		}
	}
	packet_counter++;
	if(header->len == 0){
		return STATUS_OK;
	}

	result = STATUS_ERROR;
	while(result != STATUS_OK){
		result = sendDataBody( header, data, timeout);
		if(result != STATUS_OK)
			continue;
		HeaderPack receivedHeader;
		result = receiveDataHeader( &receivedHeader, timeout);
		if(result != STATUS_OK)
			continue;

		if(verifyDataHeader(&receivedHeader) != STATUS_OK ||
				receivedHeader.messageCode != ACK /*||
				receivedHeader.num != header->num + 2*/){
			result = STATUS_ERROR;
			continue;
		}
	}
	return result;
}

Status sendAck(uint32_t num, uint32_t timeout){
	HeaderPack header;
	header.messageCode = ACK;
	header.len = 0;
	header.num = num;
	return sendDataHeader( &header, timeout);
}

Status receiveData(uint8_t * buff, HeaderPack * header, uint32_t timeout){
	Status result = STATUS_ERROR;

	while(result != STATUS_OK){
		result = receiveDataHeader( header, timeout);
		if(result != STATUS_OK || verifyDataHeader(header) != STATUS_OK/*|| header->num != packet_counter*/){
			result = STATUS_ERROR;
			continue;
		}
		/*if(header->num < packet_counter){
			sendAck( packet_counter, timeout);
			result = HAL_ERROR;
			continue;
		}*/
		sendAck( header->num + 1, timeout);
	}

	packet_counter++;
	if(header->len == 0){
		return STATUS_OK;
	}
	result = STATUS_ERROR;
	while(result != STATUS_OK){
		uint32_t crc;
		result = receiveDataBodyWithCRC(buff, header, &crc, timeout);
		if(result != STATUS_OK || crc32((char*)buff, header->len) != crc){
			result = STATUS_ERROR;
			continue;
		}
		sendAck( header->num + 2, timeout);
	}
	packet_counter++;
	return HAL_OK;
}


void askForNextBlock(uint32_t from, uint32_t to, uint32_t timeout){
	Pair body;
	body.from = from;
	body.to = to;
	HeaderPack header;
	makeHeader(&header, REQUEST, sizeof(body) - sizeof(uint32_t));
	sendData(&header, (uint8_t*)&body, timeout);
}
void sendReadyToNextCommand(uint32_t timeout){
	HeaderPack header;
	makeHeader(&header, NEXT, 0);
	sendData(&header, NULL, timeout);
}

Status HAL_printf(const char *format, ...) {
	char buff[256] = {0};
	va_list arg;
	va_start(arg, format);
	vsprintf(buff, format, arg);
	HeaderPack header;
	makeHeader(&header, STR, strlen(buff));
	Status result = sendData( &header, (uint8_t *)buff, 3000);
	va_end(arg);
	return result;
}

Status startSession() {
	uint8_t code = 0xAE;
	Status res = transmit(&code, sizeof(code), 500);
	if(res != STATUS_OK){
		return res;
	}
	return receive(&code, sizeof(code), 500);
}
