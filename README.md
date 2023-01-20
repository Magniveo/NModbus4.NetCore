# NModbus4.NetCore

This library is driven from this library https://github.com/NModbus4/NModbus4
with three changes.

1. This library is targeting .NET Core framework while the original targeting .NET Framework.
2. This library supports the SerialPort out-of-the-box while the original library needs some configuration to support that optionally (by Defining the Compile-time constant).
3. This library is simplified by removing the unit testing which exists in the original library
4. Added Work serial over TCP. (RTU over TCP) 
