@echo off
echo == Checking out log4net ==
svn checkout http://svn.apache.org/repos/asf/logging/log4net/trunk Log4Net
echo.

rem echo == Checking out sqlite ==
rem fossil clone http://system.data.sqlite.org/ sqlite.repo
rem mkdir System.Data.SQLite >NUL 2>NUL
rem cd System.Data.SQLite
rem ..\fossil open ..\sqlite.repo
rem cd ..
rem fossil sync

echo.
echo Finished.
rem pause