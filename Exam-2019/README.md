#Exam 2019
##Main part
During this homework we will be creating a multi-threaded file download and multi-threaded processing.

> **Notice on framework and software version:**
> 1. The project must be implemented in Microsoft C# language
> 2. Please download and install [Visual Studio 2017 Community](https://www.visualstudio.com/en-us/downloads/download-visual-studio-vs.aspx "Vs2017 Community Download") to perform the homeworks.
> 3. Use one of the following .NET Framework version: 4.6.2 or higher
 
Our exam task will be built around analyzing the "ՀՀ ԱԶԳԱՅԻՆ ԺՈՂՈՎԻ ԸՆՏՐՈՒԹՅՈՒՆՆԵՐ" registry data published by the Police of RA.
The files are available [here](http://www.police.am/Cucakner/cucakner/ "here") **(Please notice the use of capital "C" in the link provided.)**
 
You must write a downloader which will download all the Microsoft Excel files located in link.
All the files must be placed in a single local directory of your computer.
 
The file downloader must provide the following functionality:

1. All the files must be placed in a single local directory of your computer.
2. The file download must be done in a manner that will use all the resources of the system in a most optimal fashion.
3. Think of a smart way for discovering the number of files in the sun directories.
4. Provide GUI interface (WinForms or WPF) with a progress bar that illustrates the progress of the overall download.
5. Provide a cancelation option for overall download operation.

##Bonus part
You can earn bonus points for implementing the following features:
1. Implement good exception handling.
2. Implement I/O operation in a async fashion.

## Additional Part
You must write a Excel file parser which perform statistical calculations based on the info from Excel registry files.
 
The file parser must provide the following functionality:

1. Calculate average age of citizens of RA
2. Find the top 3 months with maximum numbers of birthdays
3. Find the top 5 common names and/or surnames
4. Find top 5 mostly uncommon names and/or surnames. 

To accomplish this you must download and install the following [Microsoft OpenXML SDK 2.5](https://www.microsoft.com/en-us/download/details.aspx?id=30425 "Microsoft OpenXML SDK 2.5"). 
Also the repo contains helper classes that you can use for Excel file processing.

The classes are the following:

1. **Citizen.cs** Contians the entity model for Citizen of RA
2. **Converter.cs** Contians an ANSI to Unicode converter for Armenian language. If the source data is in unicode no need to use this.
3. **ExcelHelper.cs** Contians helper functions for reading the Excel file.
4. **ReferendumProcessor.cs** Contains the core lgoic for processing an Excel file and returns a list of Citizens

> **Note:**
>You can do the processing using your own in-memeory constructs or parse all the data and fill it in a DB and perform queries against that db.

 

