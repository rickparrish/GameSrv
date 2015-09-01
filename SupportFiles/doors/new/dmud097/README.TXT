DOORMUD v0.97   [16bit/DOS]
=-=-=-=-=-=-=-=-=-=-=-=-=-=
Copyright 2001 Evan Elias.

This is the Sysop Documentation file for DoorMUD.  This document is also
available online in hypertext form at http://dmud.thebbs.org/sysdocs.htm.


Upgrading from a previous version
---------------------------------
If you already have a previous version of DoorMUD installed on your BBS,
upgrading to the newest release is easy.  Simply unzip the new archive to your
old DoorMUD directory, overwriting older files as needed.  


Running DoorMUD on Worldgroup
-----------------------------
Please see http://dmud.thebbs.org/wg/index.html for information on running
DoorMUD on the MajorBBS/Worldgroup BBS software.  You will also need to use
the 32-bit (Windows) version of DoorMUD instead of this DOS one.


==============================================================================


Introduction 
~~~~~~~~~~~~
In the past, the only decent BBS MUDs (multi-user dungeons) were ridiculously
expensive and ran exclusively on MBBS/Worldgroup boards.  They were geared
solely towards professional, pay-for-access BBS's.  Meanwhile, no lower-cost
alternatives were available.

DoorMUD aims to change all that.  It is an affordable, high-quality MUD game
that will run on any BBS software capable of running doorgames.  DoorMUD's
features include real-time multi-node player interaction, a text-style command
interface, and a huge room-based game world for players to explore.  It is
actively supported, with new versions coming out on a regular basis.  Several
add-on products for the game are also currently being developed to expand the 
game world and add new features.


==============================================================================


Contents
~~~~~~~~
1. Support features
2. Disclaimer
3. Setup
4. Running CONFIG.EXE
5. Registration
6. Misc


==============================================================================


1. Support Features
~~~~~~~~~~~~~~~~~~~
- Can use built-in serial I/O, or can perform I/O through use of a fossil
  driver.  If a fossil driver is present, it will be detected automatically.
- Supports most common dropfile formats, including: chain.txt, dorinfo*.def,
  exitinfo.bbs, sfdoors.dat, callinfo.bbs, door.sys, and tribbs.sys.
- Smoothly supports use of telnet servers (COM/IP, Netmodem, etc), on any
  comm port up through COM99.
- Real-time multinode interaction -- players can easily communicate with each
  other on-line, and can join up in teams to defeat more difficult monsters.
- Lots of innovative sysop-friendly features to make your life easier, 
  including automatic daily backups and the ability to un-do game resets.
- Demo version allows training up through level 7, allowing users to get a 
  good feel for the game. Registration for the basic (4-line) version costs
  only $20.


==============================================================================


2. Disclaimer - Agreement
~~~~~~~~~~~~~~~~~~~~~~~~~
Users of this program must accept the following:

DOORMUD is supplied as is.  The author disclaims all warranties, expressed or
implied.  The author assumes no liability for damages, direct or 
consequential, which may result from the use of DOORMUD.

You may not modify or disassemble this software, in whole or in part.  Use of
decompilers, hex editors, etc on DOORMUD is strictly forbidden.


==============================================================================


3. Setup
~~~~~~~~
In order to set up DoorMUD, perform the following steps:

Step 1:  Create a new directory just for DoorMUD, and copy all of the DoorMUD
         files to this directory.  (You may have already done this.)
Step 2:  Copy the file RUNMUD.BAT to wherever you usually put door batch
         files -- typically, your main BBS directory.
Step 3:  Edit RUNMUD.BAT so that the directories used are correct for your 
         system.  Remarks in the file will tell you which lines to fix.
Step 4:  Run CONFIG.EXE in DoorMUD's directory.  More info on CONFIG.EXE
         can be found in the next section.
Step 5:  Set up your BBS so it runs DoorMUD by calling RUNMUD.BAT.  You must
         pass the node number and location of the dropfile to the batch file,
         as follows:

  The BBS must add parameters when running RUNMUD.BAT in the form

    runmud.bat -n # -d XXX

  where # is the node number, and XXX is the location of the dropfile.  For 
  example, if the caller is on node 2 and the dropfile is at "c:\bbs\node2", 
  you would want the BBS to call "runmud.bat -n 2 -d c:\bbs\node2".

  Most BBS systems can automate the above process with special flags.  
  For example, a system running Renegade BBS should be set to use
  "runmud.bat -n %n -d %o".  Renegade automatically replaces "%n" with the 
  node number and "%o" with the dropfile location.  

  Some BBS programs, like MysticBBS, are able to pass the node number but not
  the dropfile location.  In this situation, use the node number to specify
  the dropfile directory.  With MysticBBS, for example, try using
  "runmud.bat -n %3 -d c:\mystic\temp%3\", assuming "c:\mystic" is your BBS
  directory.

  Please consult the documentation for your BBS software to determine what 
  flags to use in order to properly pass the node number and dropfile location.


After these steps have been completed, test out DoorMUD to make sure it is
running properly.  Testing the game remotely is recommended, but just try it
out locally if you want.  To run DoorMUD in local mode, use the syntax
"dmud -l -node 1" -- note that for local (non-BBS) play use DMUD.EXE directly
instead of calling RUNMUD.BAT.


==============================================================================


4. Running CONFIG.EXE
~~~~~~~~~~~~~~~~~~~~~
The first time you run CONFIG.EXE, you will be prompted for your real name.
The game needs this for registration purposes.

From CONFIG.EXE, you can change the following game settings:

1. Dropfile location:  Specifies the directory where the game should look for
   the user dropfile.  You will only need this option if your BBS has no way 
   of sending the dropfile location to RUNMUD.BAT on the command line.  Most
   sysops will never need to use this option.

2. Monster Fights per Day:  Controls how many monster fights a player gets
   per day.  Every time a player kills a monster, one monster fight is used up.
   Once a player runs out of monster fights, they can still continue to kill
   more monsters if desired, but they won't gain any experience or gold for 
   it.  The default is 35 monster fights per day, which will entertain an
   experienced player for about 15 to 25 minutes per day.  

3. Maximum Accumulated Fights:  Unlike most other doorgames, unspent monster
   fights in DoorMUD add up from day to day.  This way, your users do not have
   to play every single day in order to maintain a high ranking.  This setting
   controls how many unused fights a player can accumulate; the default is
   100.  It is recommended that you set it to about 3 times as high as the
   daily fights setting.  But if you wish to just turn off fight accumulation
   entirely, set it to the same number as the daily fights setting.

4. Reset the game:  Totally resets the entire game, deleting all players and
   returning all items to their original location.  This option is not 
   available the first time you run CONFIG.EXE; there is no need to select it
   when you first install the door.

5. Undo previous reset:  Restores the game files prior to the previous reset.
   This option is only available if the game has been reset before.


CONFIG.EXE also shows you your DoorMUD Pre-registration Code.  This is used
for registration purposes.  Please see section 5 for more information.


==============================================================================


5. Registration
~~~~~~~~~~~~~~~
DoorMUD is shareware.  In the unregistered version, players may not advance 
past level 7, and can only explore about 700 of the 2100+ rooms in the game 
world.  If you register the game, your users can advance to any level
and can access the entire game world.  

Registration for the basic (4 line) version costs $20 US.  DoorMUD add-on 
products are now also available at an additional charge.  Please see 
ADDONS.TXT for more information.  


HOW TO REGISTER ON-LINE:
Registering on-line is the fastest and most convenient method to purchase
DoorMUD.  Please see http://dmud.thebbs.org/register.htm for more information.


HOW TO REGISTER BY POSTAL MAIL:
1.  Print and fill out the registration form (REGISTER.TXT).  If you do not
    have a printer, you may handwrite the information.  Be sure to include
    your Preregistration Code.  This can be found by running CONFIG.EXE.
    Copy it EXACTLY as it appears.  
2.  Include a check or money order for $20 US.  If using a check, make it
    payable to "Evan Elias".
3.  Put the payment and registration form in an envelope and mail it to
    this address:

      Evan Elias
      100 Crestwood Drive
      Lansdale, PA 19446-1702
      USA

If you pay with a personal check, please allow an extra week for the check to
clear.

If you are sending registration via postal mail after August 25, 2001, 
please send an e-mail to RhythmNp@aol.com first to confirm that the above 
mailing address is still accurate!

==============================================================================


6. Misc
~~~~~~~

User license:
  This version of DoorMUD allows up to 4 simultaneous users/nodes to play the
  game at once.  This should be sufficient for most hobbyist boards.  But if
  you run a larger MUD-oriented BBS, you may be interested in the DoorMUD 
  Professional add-on, which allows for up to 16 simultaneous users.  Please
  see ADDONS.TXT for more information.

Monster Fights:
  There is intentionally no setting to allow for unlimited monster fights.
  However, you can set the Daily Monster Fights setting as high as 1000 if
  desired, which would allow for many hours of gameplay per day.  Setting it
  anywhere near this high is definitely NOT recommended though, unless you
  are using the DoorMUD Professional add-on to increase the experience point
  tables.  Otherwise, high Monster Fight settings will allow players to gain
  levels much too quickly.  In the basic version of DoorMUD, it is HIGHLY
  recommended that you leave the Monster Fights at the default setting of 35
  per day.

Automatic Backups:
  DoorMUD automatically makes backups of its vital data files on a regular
  basis, usually once per day.  If your data files ever become corrupted, 
  copy the three *.bak files (CREATURE.BAK, ITEM.BAK, CONFIG.BAK) over the
  *.dat files of the same name, to revert to the backups.  These backups are
  made just as a precaution; data file corruption is extremely rare.

USERINFO.TXT:
  This text file lists the real name of each user in the game, and shows how
  many days it has been since the user last played.  It is updated daily, when
  the game runs maintenance.  This is not a score file; it is intended to
  provide the Sysop with information as to which users are playing the game.

Command line options:  
 -D or -DROPFILE  - Door information file directory and/or filename.
 -N x or -NODE x  - Sets the node number to use.
 -L or -LOCAL     - Causes door to operate in local mode, without requiring a
                    door information (drop) file.  Please note that you should
                    just use DMUD.EXE directly for local play, instead of
                    calling RUNMUD.BAT.
 -SILENT          - Operate in silent mode, with no local display.
 -NOFOSSIL        - Disables use of FOSSIL driver, even if available.
 -MAXTIME x       - Sets the maximum number of minutes that any user will be
                    permitted to access the door, regardless of time left.
 -NOINTRO         - Skips the game's ANSI intro screen.
 -HELP            - Lists several other available command lines options for use
                    when the dropfile isn't working properly or isn't present.



Support:
-=-=-=- 
Please consult one of the following sources if you are having difficulty setting
up the game or if you have any questions, comments, ideas, etc --

 - Visit the official DoorMUD webpage:  http://dmud.thebbs.org
 - E-mail the game's programmer:        rhythmnp@aol.com

The web page contains many resources for players and sysops alike.  You may
want to give the Sysop FAQ a look if you are having difficulty with the game.



Thank You's:
-=-=-=-=-=-
Beta testing   -- Kevin Brown, Barry Bogart, Bryan Housel, Jeff Miller
Web Hosting    -- Mike Ehlert
ANSI graphics  -- Outworld Arts
16-bit Doorkit -- Brian Pirie


<End of file>