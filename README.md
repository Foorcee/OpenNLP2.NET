# OpenNLP 2.X for .NET

OpenNLP2.NET is a .NET-compatible port of Apache OpenNLP, based on the original [OpenNLP.NET](https://github.com/sergey-tihon/OpenNLP.NET) project by sergey-tihon.

This repository enables the use of OpenNLP versions greater than 2.0 with .NET by addressing compatibility limitations introduced with newer Java versions.

## Key Features and Modifications

### Support for OpenNLP > 2.0
Apache OpenNLP 2.0 and later versions require a newer Java version. However, the current IKVM version supports only Java 8. To make newer OpenNLP versions compatible, this project uses the [JvmDowngrader](https://github.com/unimined/JvmDowngrader) to recompile OpenNLP JAR files back to Java 8, making them usable in .NET applications while retaining access to the latest NLP features.

### Updated IKVM Version
This project also integrates the latest available IKVM version for optimal performance and compatibility in .NET environments.
