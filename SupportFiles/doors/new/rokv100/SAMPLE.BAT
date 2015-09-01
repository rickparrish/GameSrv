REM ** SAMPLE BATCH FILE FOR SINGLE-LINE BBS **

@ECHO OFF                       - No display what is happening.
<drive>:                        - Change to the BBS Drive.
CD\<rokdir>                     - Change Dir to the ROK Directory.
COPY \<bbsdir>\<dropfile>       - Copy Dropfile from BBS Dir into ROK Dir.
NOW_OFF.EXE                     - Turn OnNow Flag off for all players.
ROK.EXE 1                       - Run ROK on node #1.
CD\<bbsdir>                     - Return to the BBS Directory.
EXIT                            - Exit back to the BBS.

REM ** SAMPLE BATCH FILE FOR MUTLI-NODE BBS **

@ECHO OFF                       - No display what is happening.
<drive>:                        - Change to the BBS Drive.
CD\<rokdir>                     - Change Dir to the ROK Directory.
ROK.EXE * /P<pathtodropfile>    - Execute ROK on a "*" for the node number
                                  (consult your BBS Documentation for info on
                                  how to pass node numbers to command lines).
                                  /P<path> to point ROK to a directory to look
                                  for the dropfile -- must have on multi-node
                                  BBS's.
CD\<bbsdir>                     - Return to the BBS Directory.
EXIT                            - Exit back to the BBS.

Devon Brooks - Blazing Fire Software
End of Realm of Kisom v0.95c Sample Batch File.
