FROM microsoft/aspnet
RUN mkdir c:\tools
RUN mkdir c:\Package

WORKDIR /tools
COPY tools .
RUN powershell Start-Process msiexec.exe -ArgumentList '/i c:\tools\WebDeploy_3_6_amd64_en-US.msi /qn' -Wait

WORKDIR /Package
COPY Package .
RUN Archive.deploy.cmd, /Y

WORKDIR /
RUN rd /s /q c:\tools
RUN rd /s /q c:\Package
EXPOSE 80