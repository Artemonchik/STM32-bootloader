#include <stdint.h>
#include <stdio.h>

#ifndef INC_CHECKSUM_H_
#define INC_CHECKSUM_H_
#define CRC_SIZE 4

uint32_t crc32(const char *s,size_t n);
int validate_program(uint8_t * address, size_t n, uint32_t expected_crc);
#endif /* INC_CHECKSUM_H_ */
