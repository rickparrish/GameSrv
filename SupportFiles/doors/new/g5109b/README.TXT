
                              ÄÄÍÍ Galaxy 5 ÍÍÄÄ
                      Copyright (C) 1996 Thomas J. Smith
                             Important Information

                   ** Galaxy 5 does NOT need a demo key! **

     ÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄ
      Please be advised, that this is a WIDE BETA release of Galaxy 5!
      Which means it is still being tested for bugs! If you find any
      bugs, you are encouraged to tell me about them! Shown at the end
      of this file is my contact info. Please use the BUGFOUND.TXT file
      to report bugs. Just fill it out answering all of the info that
      you can. Thanx.
     ÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄ
                                     -=ğ=-
     ÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄ
      NOTE: If you plan on creating your own ANSI menus for the game,
      you must save them in 100 lines or less!!
     ÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄ
                                     -=ğ=-
     ÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄ
      In G5 Setup, you should first choose "Setup Nodes" to setup all
      of the nodes you will be running the game on (including node 0 if
      you have one), then choose "Setup Galaxy 5."
     ÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄ
      In the batch file(s) that you will be running the game with from
      your BBS, you *MUST* change to your Galaxy 5 directory, and CALL
      the G5.BAT file with the first parameter being the node number
      the caller is on. Here is an example for node 2,

      @ECHO OFF
      CD\DOORS\GALAXY5
      CALL G5.BAT 2
      CD\BACK\TO\BBS\OR\WHATEVER

      Replacing 2 with the proper node number. If you run a Wildcat!
      system (or any other system that uses node ID variables), then you
      can use the command CALL G5.BAT %WCNODEID% (or whatever your BBS
      software calls the node ID variable in between the % signs).
     ÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄ
                                     -=ğ=-
     ÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄ
      There are a few features that Galaxy 5 has, which you may want to
      make note of.
     ÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄ
      1 ş By default, Galaxy 5 creates a message to all new players
          saying, "Welcome to the Galaxy!" If you want to customize this
          message, simply create a text file containing the text you
          would like inserted into this message, and it will be used.
          Take note that you must also supply the name of this file in
          G5 Setup under "Setup Galaxy 5" in the field named, "SysOps
          welcome message." This file can contain ANSI codes, Galaxy 5
          codes, or just plain ASCII text. Each new player will recieve
          a message containing the text in the file you specify from the
          "Messenger Droid" upon their first logon.
     ÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄ
      2 ş Galaxy 5 will display a "SysOps Bulletin" to all callers upon
          entering the game. In order for it to do so, you must tell
          Galaxy 5 where to find this file in G5 Setup under "Setup
          Galaxy 5" in the "SysOps Bulletin" field.  This file will be
          displayed to the caller as the "SysOps Bulletin." This file
          also can contain ANSI codes, Galaxy 5 codes, or just plain
          ASCII text. It will be shown to players after reading "Today
          In The Galaxy" when they first enter the game.
     ÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄ
      3 ş Galaxy 5 will run a LOGON.BAT file if it exists in your home
          Galaxy 5 directory. You can use this batch file to run an
          Xport, random menu switching program, maintenance, etc. Just
          make sure that the program you are running from the LOGON.BAT
          file exits without asking for input from the local console!
          Otherwise, the game will appear to be locked up from the
          callers view.
     ÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄ
      4 ş Other batch files can be run also if they simply exist in your
          home Galaxy 5 directory. These are described below...

          PRELOG.BAT - G5 "shells" out and runs this one. NOTE that
                       anything ran from this batch file must NOT
                       require the dropfiles that G5 creates for Xport
                       use, as this file is executed BEFORE G5 even
                       takes recognition of the player entering the
                       game!

          LOGOFF.BAT - This one is the same as PRELOG.BAT, except that
                       it is ran just BEFORE the game completely exits
                       back to the BBS. Again, anything ran from this
                       file must NOT need to read from the G5 drop files
                       as they will have been already erased by the time
                       this file is executed!
     ÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄ
                                     -=ğ=-
     ÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄ
      If you have any problems installing the game, then refer to
      INSTALL.TXT. This file contains detailed instructions on how to
      install the game using INSTALL.EXE, and installing the game
      manually, as well as quick installation instructions.
     ÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄ
      If you have problems running the game, please look over
      GALAXY5.DOC and any other text files included with this package
      (most files are inside the G5.ZIP file that is inside the
      G5xxx.ZIP file), before contacting me. This may save you a long
      distance call!
     ÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄ
                                 CONTACTING ME

      My BBS   : The Last Call BBS (313) 295-6284
      FidoNet  : 1:2410/903
      Internet : tsmith2@voyager.net
      WWW      : http://www.geocities.com/SiliconValley/Heights/2233
      Voice    : (313) 295-6287

      I also frequent these echos on FidoNet,
                                             DOORGAMES
                                             DOORWARE
                                             ON_LINE_GAMES
     ÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄÄ
