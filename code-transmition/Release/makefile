################################################################################
# Automatically-generated file. Do not edit!
################################################################################

-include ../makefile.init

RM := rm -rf

# All of the sources participating in the build are defined here
-include sources.mk
-include Drivers/STM32F3xx_HAL_Driver/Src/subdir.mk
-include Core/Startup/subdir.mk
-include Core/Src/subdir.mk
-include subdir.mk
-include objects.mk

ifneq ($(MAKECMDGOALS),clean)
ifneq ($(strip $(S_DEPS)),)
-include $(S_DEPS)
endif
ifneq ($(strip $(S_UPPER_DEPS)),)
-include $(S_UPPER_DEPS)
endif
ifneq ($(strip $(C_DEPS)),)
-include $(C_DEPS)
endif
endif

-include ../makefile.defs

# Add inputs and outputs from these tool invocations to the build variables 
EXECUTABLES += \
code-transmition.elf \

SIZE_OUTPUT += \
default.size.stdout \

OBJDUMP_LIST += \
code-transmition.list \

OBJCOPY_BIN += \
code-transmition.bin \


# All Target
all: code-transmition.elf secondary-outputs

# Tool invocations
code-transmition.elf: $(OBJS) $(USER_OBJS) C:\Users\Artem\STM32CubeIDE\STM32-bootloader\code-transmition\STM32F303VCTX_FLASH.ld
	arm-none-eabi-gcc -o "code-transmition.elf" @"objects.list" $(USER_OBJS) $(LIBS) -mcpu=cortex-m4 -T"C:\Users\Artem\STM32CubeIDE\STM32-bootloader\code-transmition\STM32F303VCTX_FLASH.ld" --specs=nosys.specs -Wl,-Map="code-transmition.map" -Wl,--gc-sections -static -L"C:\Users\Artem\STM32CubeIDE\workspace_1.4.0\code-transmition" -Xlinker --no-keep-memory --specs=nano.specs -mfpu=fpv4-sp-d16 -mfloat-abi=hard -mthumb -Wl,--start-group -lc -lm -Wl,--end-group
	@echo 'Finished building target: $@'
	@echo ' '

default.size.stdout: $(EXECUTABLES)
	arm-none-eabi-size  $(EXECUTABLES)
	@echo 'Finished building: $@'
	@echo ' '

code-transmition.list: $(EXECUTABLES)
	arm-none-eabi-objdump -h -S $(EXECUTABLES) > "code-transmition.list"
	@echo 'Finished building: $@'
	@echo ' '

code-transmition.bin: $(EXECUTABLES)
	arm-none-eabi-objcopy  -O binary $(EXECUTABLES) "code-transmition.bin"
	@echo 'Finished building: $@'
	@echo ' '

# Other Targets
clean:
	-$(RM) *
	-@echo ' '

secondary-outputs: $(SIZE_OUTPUT) $(OBJDUMP_LIST) $(OBJCOPY_BIN)

.PHONY: all clean dependents
.SECONDARY:

-include ../makefile.targets
