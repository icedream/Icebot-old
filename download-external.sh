#!/bin/sh
echo == Checking out log4net ==
svn checkout http://svn.apache.org/repos/asf/logging/log4net/trunk Log4Net

#echo == Checking out sqlite ==
#fossil clone http://system.data.sqlite.org/ sqlite.repo
#mkdir System.Data.SQLite >/dev/null 2>/dev/null
#cd System.Data.SQLite
#../fossil open ../sqlite.repo
#cd ..
#fossil sync

echo Finished.
