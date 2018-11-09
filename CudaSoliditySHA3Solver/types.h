#pragma once

#include <array>

static const unsigned short UINT32_LENGTH{ 4u };
static const unsigned short UINT64_LENGTH{ 8u };
static const unsigned short SPONGE_LENGTH{ 200u };
static const unsigned short ADDRESS_LENGTH{ 20u };
static const unsigned short UINT256_LENGTH{ 32u };
static const unsigned short MESSAGE_LENGTH{ UINT256_LENGTH + ADDRESS_LENGTH + UINT256_LENGTH };
static const unsigned short NONCE_POSITION{ UINT256_LENGTH + ADDRESS_LENGTH + ADDRESS_LENGTH };

typedef std::array<uint8_t, ADDRESS_LENGTH> address_t;
typedef std::array<uint8_t, UINT256_LENGTH> byte32_t;
typedef std::array<uint8_t, MESSAGE_LENGTH> message_t;
typedef std::array<uint8_t, SPONGE_LENGTH> sponge_t;

typedef struct _message_s
{
	byte32_t				challenge;
	address_t				address;
	byte32_t				solution;
} message_s;

typedef union _message_ut
{
	message_t				byteArray;
	message_s				structure;
} message_ut;

typedef union _sponge_ut
{
	sponge_t				byteArray;
	uint64_t				uint64Array[25];
} sponge_ut;