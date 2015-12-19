#Assignment 2
 
##Main part
During this homework we will be performing calculations based on the refferendum files that you have downloaded during Homework #1. 

> **Notice on framework and software version:**
> 1. The project must be implemented in Microsoft C# language
> 2. Please download and install [Visual Studio 2015 Community](https://www.visualstudio.com/en-us/downloads/download-visual-studio-vs.aspx "Vs2015 Community Download") to perform the homeworks.
> 3. Use one of the following .NET Framework version: 4.5.1, 4.5.2, 4.6
 
Our homeworks will be built around analyzing the "Hanraqve 2016" registry data published by the Police of RA.
The files are available [here](http://www.police.am/Hanraqve/ "here") **(Please notice the use of capital "H" in the link provided.)**
 
You must write a Excel file parser which perform statistical calculations based on the infor from "Hanraqve 2016" registry data.
 
The file parser must provide the following functionality:

1. Calculate average age of citizens of RA
2. Find the top 3 months with maximum numbers of birthdays
3. Find the top 5 common names and/or surnames
4. Find top 5 mostly uncommon names and/or surnames. 

To accomplish this you must download and install the following [Microsoft OpenXML SDK 2.5](https://www.microsoft.com/en-us/download/details.aspx?id=30425 "Microsoft OpenXML SDK 2.5"). 
Also the repo contains helper classes that you can use for Excel file processing.

The classes are the following:

1. **Citizen.cs** Contians the entity model for Citizen of RA
2. **Converter.cs** Contians an ANSI to Unicode converter for Armenian language
3. **ExcelHelper.cs** Contians hleper functions for reading the Excel file.
4. **ReferendumProcessor.cs** Contains the core lgoic for processing an Excel file and returns a list of Citizens

> **Note:**
>You can do the processing using your own in-memeory constructs or parse all the data and fill it in a DB and perform queries against that db.

##Bonus Points
1. Rework and combine the Assignment#1 and Assignment#2 into a single pipeline style applications
2. Try to use the TPL DataFlow library for file downlaod and processing.

##Submission and Questions
The deadline for the submission of the project is 25th December 2015 6:00 AM. The project must be submitted via email before the deadline. 

If you have any questions, don't hesitate and contact me by email, and I'll get back to you.
 

