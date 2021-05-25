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
	header->num = packet_counter;
	header->crc = computeHeaderCrc(header);
}

Status verifyDataHeader(HeaderPack * header){
	if(computeHeaderCrc(header) != header->crc){
		return STATUS_ERROR;
	}
	return STATUS_OK;
}

Status sendDataHeader(HeaderPack * header, uint32_t timeout){
	header->crc = computeHeaderCrc(header);
	Status res = transmit( (uint8_t*) (header), sizeof(HeaderPack), timeout);
	return res;
}

Status sendFullPacket(HeaderPack * header, uint8_t *data, uint32_t timeout) {
	uint32_t crc = crc32((char*)data, header->len);
	computeHeaderCrc(header);
	memmove((void *)(data + sizeof(HeaderPack)), (void *)&data[0], header->len);
	memcpy((void *) data, (void *)header, sizeof(HeaderPack));
	memcpy((void *)(data + header->len + sizeof(HeaderPack)), (void *)&crc, sizeof(crc));
	Status result = transmit(data, header->len + sizeof(HeaderPack) + sizeof(crc), timeout);

	return result;
}

// TODO: improve this
Status receiveDataHeader(HeaderPack * header, uint32_t timeout){
	memset(header, 0, sizeof(HeaderPack));
	Status res = receive((uint8_t *) header, (uint16_t) sizeof(HeaderPack), timeout);
	return res;
}

Status receiveFullPacket(uint8_t * buff, HeaderPack * header, uint32_t * crc, HeaderPack * outputHeader, uint32_t timeout){
	memset(buff, 0, sizeof(HeaderPack) + header->len + sizeof(uint32_t));
	Status res = receive(buff, sizeof(HeaderPack) + header->len + sizeof(uint32_t), timeout);
	*outputHeader = *((HeaderPack *) buff);
	*crc = *((uint32_t *)(&buff[header->len + sizeof(HeaderPack)]));
	memmove(buff, buff + sizeof(HeaderPack), header->len + sizeof(uint32_t));
	return res;
}

Status sendData(HeaderPack * header,
		uint8_t *data, uint32_t timeout){
	Status result = STATUS_ERROR;
	while(result != STATUS_OK){
		result = sendDataHeader(header, timeout);
		if(result != STATUS_OK)
			continue;
		HeaderPack receivedHeader = {0};
		result = receiveDataHeader(&receivedHeader, timeout);
		if(verifyDataHeader(&receivedHeader) != STATUS_OK){
			result = STATUS_ERROR;
			continue;
		}
		// TODO: may be infinite cycle
		if(packet_counter == receivedHeader.num + 1){
			sendAck(packet_counter, timeout);
			continue;
		}
		result = STATUS_OK;
	}
	packet_counter++;
	if(header->len == 0){
		return STATUS_OK;
	}
	makeHeader(header, header->messageCode, header->len);
	result = STATUS_ERROR;
	while(result != STATUS_OK){
		result = sendFullPacket(header, data, timeout);
		if(result != STATUS_OK)
			continue;
		HeaderPack receivedHeader = {0};
		result = receiveDataHeader(&receivedHeader, timeout);
		if(verifyDataHeader(&receivedHeader) != STATUS_OK){
			result = STATUS_ERROR;
			continue;
		}
		if(packet_counter == receivedHeader.num + 1){
			sendAck(packet_counter, timeout);
			continue;
		}
		result = STATUS_OK;
	}
	packet_counter++;
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
		result = receiveDataHeader(header, timeout);
		if(verifyDataHeader(header) != STATUS_OK){
			result = STATUS_ERROR;
			continue;
		}
		if(packet_counter == header->num + 1){
			sendAck(packet_counter, timeout);
			continue;
		}
		sendAck(packet_counter + 1, timeout);
		result = STATUS_OK;
	}

	packet_counter++;
	if(header->len == 0){
		return STATUS_OK;
	}
	result = STATUS_ERROR;
	while(result != STATUS_OK){
		HeaderPack receivedHeader;
		uint32_t crc;
		result = receiveFullPacket(buff, header, &crc, &receivedHeader, timeout);
		if(verifyDataHeader(&receivedHeader) != STATUS_OK){
			result = STATUS_ERROR;
			continue;
		}
		if(packet_counter == receivedHeader.num + 1){
			sendAck(packet_counter, timeout);
			continue;
		}

		if(crc32((char*)buff, header->len) != crc){
			result = STATUS_ERROR;
			continue;
		}
		sendAck(packet_counter + 1, timeout);
		result = STATUS_OK;
	}
	packet_counter++;
	return STATUS_OK;
}

// TODO: исправить это дерьмо
void askForNextBlock(uint32_t from, uint32_t to, uint32_t timeout){
	Pair body[10];
	body[0].from = from;
	body[0].to = to;
	HeaderPack header;
	makeHeader(&header, REQUEST, sizeof(body[0]) - sizeof(uint32_t));
	sendData(&header, (uint8_t*)&body[0], timeout);
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
