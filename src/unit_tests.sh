#!/bin/bash

mono packages/Machine.Specifications-Signed.0.5.12/tools/mspec-clr4.exe Hydrospanner.UnitTests/bin/Debug/Hydrospanner.UnitTests.dll
mono packages/Machine.Specifications-Signed.0.5.12/tools/mspec-clr4.exe Hydrospanner.IntegrationTests/bin/Debug/Hydrospanner.IntegrationTests.dll