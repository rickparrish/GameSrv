DOORMUD v0.97  [Door32/Windows]
=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
Copyright 2002 Evan Elias.


***** Public Beta Test Release ***** Revision B ***** 1/20/02 ******************
* Note -- this is still a beta version. It seems to run fine on Mystic's
* TSERVER.EXE.  It runs well on WildCat Winserver under Windows NT/2k, but
* hasn't been tested for Winserver under Win 95/98/ME. It hasn't been tested 
* much under EleBBS either.  Please report any bugs to RhythmNp@aol.com.  
* Thanks!
********************************************************************************


This is the Sysop Documentation file for DoorMUD.  This document is also
available online in hypertext format at http://dmud.thebbs.org/sysdocs.htm ,
complete with configuration screenshots!  You may find that version of the
docs to be easier to use.


Platform notes:
 - This version of DoorMUD is a special 32-bit build for BBS software 
   supporting Door32.sys, such as WildCat Winserver, and the 32-bit
   versions of MysticBBS and EleBBS.  
 - If you are running Synchronet, do NOT use this version.  There is a 
   separate, Synchronet-specific (XTRN.DAT) release of DoorMUD with extra
   features such as Synchronet global commands.  See the DoorMUD web site for
   more information.
 - If your BBS does not support Door32.sys, please use the DOS version of
   DoorMUD instead.


Upgrading from a previous Win32 version:
   If you already have a previous Windows version of DoorMUD installed on your
   BBS, upgrading to the newest release is easy.  Simply unzip the new archive
   to your old DoorMUD directory, overwriting older files as needed.  


Upgrading from the DOS version to the Win32 version:
   If you currently have a DOS version of DoorMUD installed on your BBS, and
   you want to upgrade to this Win32 version, please consult UPGRADE.TXT.


==============================================================================


Introduction 
~~~~~~~~~~~~
DoorMUD: Land of the Forgotten is a text-based role-playing game of massive
proportions.  In an ancient, war-torn land, the demonic Warmaster of Chaos
lies locked away in a dungeon deep underground. His minions are forming armies
on the surface, preparing for the day when an evil champion will set him free.
Meanwhile, the holy forces of Order have finally forged a weapon capable of
slaying the Warmaster, but first they must find a hero powerful enough to 
wield the mighty blade. Thus, players are presented with a choice: Will they
strive to kill the Warmaster and bring Order to the land? Or shall they free
the Warmaster and doom the world to its destruction?

DoorMUD is an affordable, high-quality MUD (multi-user dungeon) that will run
on any BBS software capable of running doorgames. DoorMUD features fast-paced
real-time combat, excellent multinode interaction, a high-powered command
interface and a huge game world with over 2100+ rooms for players to explore.
It is actively supported, with new versions coming out on a regular basis.  A
World Editor add-on is also in the works, allowing Sysops to modify the game 
world and create your own new dungeons, quests, artifacts, and more!


==============================================================================


Contents
~~~~~~~~
1. Support features
2. Disclaimer
3. Setup
4. Configuration options
5. Registration
6. Misc


==============================================================================


1. Support Features
~~~~~~~~~~~~~~~~~~~
- 32-bit Windows version made for door32.sys -- runs much faster than the 
  DOS version, and has a smooth sysop display interface.
- Real-time multinode interaction -- players can easily communicate with each
  other on-line, and can join up in teams to defeat more difficult monsters.
- Lots of innovative sysop-friendly features to make your life easier, 
  including automatic daily backups and the ability to un-do game resets.
- Demo version allows training up through level 7, allowing users to get a 
  good feel for the game. Registration for the basic version costs only $20.


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


Step 2:  Set up your BBS so it runs DoorMUD by calling DMUD32D.EXE.  You must
         pass the node number and location of the dropfile to the program,
         as follows:

           dmud32d.exe -n # -d XXX

         where # is the node number, and XXX is the location of the dropfile.
         Most BBS systems can supply this data easily with special flags.
         Shown below are example command lines for specific software.
 
       * WildCat Winserver (under Windows NT and Windows 2000):
           You must run DoorMUD through a batch file.  Copy WCMUD.BAT from your
           DoorMUD directory to whatever directory you normally put door batch
           files, and edit it as directed in the batch file's comments.  Then,
           configure your BBS to run DoorMUD as "Door32 Compatible" for Door
           Type, and "WCMUD.BAT" for the Batch File.  Be sure to check the
           Multiuser toggle.

       * WildCat Winserver (under Windows 95/98/ME):
           Full setup not yet available.  You will need to call DMUD32D.EXE 
           directly, rather than using a batch file.  The command line will
           probably be something similar to: 
             c:\doormud\dmud32d.exe -n %wcnodeid% -d c:\doors\node%wcnodeid%
           That is, assuming c:\doormud is your DoorMUD directory and 
           c:\doors\node# is where your BBS puts its dropfiles. This setup has
           not yet been fully tested. If you find the correct command line for
           running DoorMUD on Winserver under Windows 95/98/ME, please email
           RhythmNp@aol.com so that these docs can be updated

       * Mystic BBS: 
           Assuming c:\doormud is your DoorMUD directory, and c:\mystic is
           your Mystic directory, use a command line of:
              c:\doormud\dmud32d.exe -n %3 -d c:\mystic\temp%3
           Set the menu command type to "D3  Door with DOOR32.SYS".
           Please note that the original release of Mystic v1.07 had a bug
           that prevents Door32 games from working properly.  You must have
           Mystic v1.07.2 or later in order for DoorMUD to run.

       * EleBBS:
           Full command line not yet available.  EleBBS may have issues running
           32-bit doors through door32.sys.  The command line should probably
           be something similar to:
              c:\doormud\dmud32d.exe -n *N -d c:\ele\NODE*N\door32.sys *W
           But this may or not work properly depending on your version of
           EleBBS and which flavor of Windows you are running.  If you are
           using a recent private testing version of EleBBS, try replacing the
           *W with *Y, which may work better.  Also, you may need to lower the
           value for Modem >> Telnet >> Start Node# in ELCONFIG since DoorMUD
           does not support node numbers higher than 99.  If you find the 
           correct command line for running DoorMUD on EleBBS, please email
           RhythmNp@aol.com so that these docs can be updated.


Step 3:  To finish the installation, you must run the door in local mode by
         executing "dmud32d.exe -l".  The game will automatically finish its
         installation the first time you run it in local mode.



*** Important Note for users of Windows 95, 98, and ME ***
The Door32 version of DoorMUD *cannot* be run from a batch file on these 
systems. Please be sure to run dmud32d.exe directly. The door should 
automatically change to its own directory on start-up, and change back to the
proper BBS directory upon exiting, so there is no need for a batch file anyway.


*** Important Note for users of Windows NT and Windows 2000 ***
You may have difficulty executing DoorMUD in local mode from a DOS command 
prompt.  If this problem occurs, please use the Start menu >> Run option to 
execute the door in local mode.  Be sure to include the full path; that is, 
in the Run window, type something like "c:\doormud\dmud32d.exe /l". 



==============================================================================


4. Configuration Options
~~~~~~~~~~~~~~~~~~~~~~~~
If you enter the door in local mode ("dmud32d.exe -l"), a special menu option
will become available that allows you to configure the game's settings.

At the top of the Configuration screen, you will see your Pre-registration
Code.  This is used for registration purposes if you wish to purchase the full
version of the game.

From the Configuration screen, you can change the following game settings:

[F] Monster Fights per Day:  
       Controls how many monster fights a player gets per day.  Every time a
       player kills a monster, one monster fight is used up.  Once a player
       runs out of monster fights, they can still continue to kill more 
       monsters if desired, but they won't gain any experience or gold for it.
       The default is 35 monster fights per day, which will entertain an
       experienced player for about 15 to 25 minutes per day.  

[M] Maximum Accumulated Fights:
       Unlike most other doorgames, unspent monster fights in DoorMUD add up
       from day to day.  This way, your users do not have to play every single
       day in order to maintain a high ranking.  This setting controls how 
       many unused fights a player can accumulate; the default is 100.  It is
       recommended that you set it to about 3 times as high as the daily fights
       setting.  But if you wish to just turn off fight accumulation entirely,
       set it to the same number as the daily fights setting.

[R] Reset the game
       Totally resets the entire game, deleting all players and returning all
       items to their original location.  Please note that there is no need to
       select this option when you first install the door.

[U] Undo previous reset
       Restores the game files prior to the previous reset. This option is 
       only available if the game has been reset before.


If you have purchased the DoorMUD Professional add-on, additional config
options are also available.  Please see the DoorMUD Professional documentation
for information on these settings.


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
-----------------------
Registering on-line is the fastest and most convenient method to purchase
DoorMUD.  Please see http://dmud.thebbs.org/register.htm for more information.



HOW TO REGISTER BY POSTAL MAIL:
------------------------------
1.  Print and fill out the registration form (REGISTER.TXT).  If you do not
    have a printer, you may handwrite the information.  Be sure to include
    your Pre-registration Code.  This can be found at the top of the
    Configuration screen (see part 4, above).  Copy it EXACTLY as it appears.
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

*** Important note:  Before sending a registration via postal mail, please
    e-mail RhythmNp@aol.com first to confirm that the above mailing address
    is still accurate!


==============================================================================


6. Misc
~~~~~~~

Sysop display window:
  By default, the Door32 version of DoorMUD provides an interactive graphical
  display window on your computer whenever one of your users is in DoorMUD.
  This window allows you to monitor the user, as well as perform sysop actions
  such as hang-up, increase/decrease time left, chat mode, etc.  Unless you are
  running DoorMUD in local mode, this window automatically starts minimized.
  If desired, you can eliminate this window entirely using the -silent command
  line option (ie, dmud32d.exe -n # -d XXX -silent).  This can be useful in
  order to prevent taskbar clutter.

User license:
  This version of DoorMUD allows up to 4 simultaneous users/nodes to play the
  game at once.  This should be sufficient for most hobbyist boards.  But if
  you run a larger MUD-oriented BBS, you may be interested in the DoorMUD 
  Professional add-on, which allows for up to 16 simultaneous users.  Please
  see ADDONS.TXT for more information.

Monster Fights:
  There is intentionally no setting to allow for unlimited monster fights.
  However, you can set the Daily Monster Fights setting as high as 1000 if
  desired, which would allow for 9+ hours of gameplay per day.  Setting it
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


Command-line options:
 -D or -DROPFILE  - Door information file directory and/or filename.
 -N or -NODE      - Sets the node number to use.
 -L or -LOCAL     - Causes door to operate in local mode, without requiring a
                    door information (drop) file.  
 -SILENT          - Operate in silent mode, with no local display window.
 -NOINTRO         - Causes the game not to display its ANSI intro screen.
 -MAXTIME x       - Sets the maximum number of minutes that any user will be
                    permitted to access the door, regardless of time left.
 -GRAPHICS        - Unless followed by 0 or N, turns on ANSI display mode.
                    Useful if, for whatever reason, the game is not detecting
                    users' ANSI settings properly.



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
32-bit Doorkit -- Brian Pirie and Rob Swindell


<End of file>