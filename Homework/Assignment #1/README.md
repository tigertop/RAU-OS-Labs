#Assignment 1
 
##Main part
During this homework we will be creating a multi-threaded file download, which will be the basis for the next homeworks and final project.

> **Notice on framework and software version:**
> 1. The project must be implemented in Microsoft C# language
> 2. Please download and install [Visual Studio 2015 Community](https://www.visualstudio.com/en-us/downloads/download-visual-studio-vs.aspx "Vs2015 Community Download") to perform the homeworks.
> 3. Use one of the following .NET Framework version: 4.5.1, 4.5.2, 4.6
 
Our homeworks will be built around analyzing the "Hanraqve 2016" registry data published by the Police of RA.
The files are available [here](http://www.police.am/Hanraqve/ "here") **(Please notice the use of capital "H" in the link provided.)**
 
You must write a downloader which will download all the Microsoft Excel files located in the sub-folders of the "Hanraqve".
All the files must be placed in a single local directory of your computer.
 
The file downloader must provide the following functionality:

1. All the files must be placed in a single local directory of your computer.
2. The file download must be done in a manner that will use all the resources of the system in a most optimal fashion.
3. Think of a smart way for discovering the number of files in the sun directories.
4. Provide GUI interface (WinForms or WPF) with a progress bar that illustrates the progress of the overall download.
5. Provide a cancelation option for overall download operation.
6. After downloading the files to a single output folder the files must be converted to Microsoft Excel 2013 (xlsx) format.

To accomplish this you must download the following [Microsoft Converter](https://www.microsoft.com/en-US/download/details.aspx?id=3 "Microsoft Converter"). Install converter on your computer. After that you can use the converter by calling the following function:
```C#
Process.Start(@"c:\Program Files (x86)\Microsoft Office\Office12\excelcnv.exe", string.Format(@" -nme -oice {0} {1}", filename, destinationFilename))
```
In the code above the string **filename** must contain the full path to the source file and the **destinationFilename** must contain the full path of the file to be created.

> **Note:**
>Please notice the Process.Start() function only starts the process and returns. You must limit the number of concurrent starts of the converter executable in-order not to oversaturate number of threads**


##Bonus part
You can earn bonus points for implementing the following features:
1. Provide cancelation for a single directory.
2. Implement good exception handling.
3. Implement I/O operation in a async fashion.

##Submission and Questions
The deadline for the submission of the project is 25th November 2015 10:00 PM. The project must be submitted via email before the deadline. 

If you have any questions, don't hesitate and contact me by email, and I'll get back to you.
 

