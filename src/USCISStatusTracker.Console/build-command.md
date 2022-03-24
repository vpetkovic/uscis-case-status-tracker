# Markdown File

- Make sure you have dotnet CLI installed. .NET CLI is included with the .NET SDK. [More info how to install it along with downlaod paths]("https://docs.microsoft.com/en-us/dotnet/core/install/windows?tabs=net60")
- Once you have dotnet cli installed, navigate to project folder using command prompt and the following
    - Make sure that you specify the `--output` path properly. 
    - Down the road if project is updated to use newer frameworks make sure you set proper option in command. Default is .NET6.0
    - Once you build based on enviroment select appropriate config file for destination directory

###DEV###

`dotnet publish USCISStatusTracker.Console.csproj --configuration Debug --framework net6.0 --output bin/Debug/singlefile --self-contained True --runtime win10-x64 --verbosity Normal /property:PublishSingleFile=True /property:IncludeNativeLibrariesForSelfExtract=True /property:DebugType=None /property:DebugSymbols=False /property:ExcludeFromSingleFile=true`

###PROD###

`dotnet publish USCISStatusTracker.Console.csproj --configuration Release --framework net6.0 --output bin/Release/singlefile --self-contained True --runtime win10-x64 --verbosity Normal /property:PublishSingleFile=True /property:IncludeNativeLibrariesForSelfExtract=True /property:DebugType=None /property:DebugSymbols=False /property:ExcludeFromSingleFile=true`