################################################################################
# Automatically-generated file. Do not edit!
################################################################################

# Add inputs and outputs from these tool invocations to the build variables 
S_SRCS += \
../Core/Startup/startup_stm32f303vctx.s 

OBJS += \
./Core/Startup/startup_stm32f303vctx.o 

S_DEPS += \
./Core/Startup/startup_stm32f303vctx.d 


# Each subdirectory must supply rules for building sources it contributes
Core/Startup/startup_stm32f303vctx.o: ../Core/Startup/startup_stm32f303vctx.s
	arm-none-eabi-gcc -mcpu=cortex-m4 -c -x assembler-with-cpp -MMD -MP -MF"Core/Startup/startup_stm32f303vctx.d" -MT"$@" --specs=nano.specs -mfpu=fpv4-sp-d16 -mfloat-abi=hard -mthumb -o "$@" "$<"

