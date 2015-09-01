type Str30 = string[30];
{*********************************************************************}
{* Player Record!                                                    *}
{*-------------------------------------------------------------------*}
{* This is the record for all 4 players                              *}
{*********************************************************************}
     Player = Record
            Team_Name : Str30;
            F_Name,
            L_Name    : Str30;
            Alias     : array [1..4] of Str30;
            Sex,
            Malaky,NPC,Drunk,
            Inn, stole, Streets, Tank,
            Ran,
            Dead,
            On_Today  : array [1..4] of boolean;
            Stg,
            Int,
            Dex,
            Con,
            Level,
            Special_Attack,
            Special_Attack_Max,
            Weapon,
            Armor     : array [1..4] of byte;
            W_Condition,
            A_Condition : array [1..4] of integer;
            HP,
            HMax      : array [1..4] of LongInt;
            Items     : Array [1..10] of Byte;
            Race,
            Class     : array [1..4] of Char;
            Exp,
            Money,
            Bank      : array [1..4] of LongInt;
            Attack,
            Defense  : array [1..4] of LongInt;
            S_Fights,
            P_Fights,
            Magic_Points : array [1..4] of Byte;
            Player_Kills : array [1..4] of Integer;
            LastOn : longint;

     End;
{
********* F I L E * L O C K I N G **************************************
The file locking used with Drakeville is the one given with the DDPLUS 7.+
package. You can get DDPLUS 7.0 from ftp://ftp.uniserve.com/pub/bbs_doors.

********* D R O P * F I L E ********************************************
You may not use any other externel drop file but the one that Drakeville will
suply. The file will be named DRAKEx.SYS where 'x' will be the node number.
Here is the content of the drop file.

Line 1 is the COM PORT
Line 2 is the IRQ
Line 3 is the PORT ADDRESS
Line 4 is the TIME LEFT
Line 5 is the USER'S FIRST NAME
Line 6 is the USER'S LAST NAME
Line 7 is the BBS GRAPHICS
Line 8 is the BAUD RATE

BBS GRAPHICS
2 = ANSI
5 = RIP

Drakeville will also create a INFO.x that holds the TEAM'S (that is on-line)
record in a binary file.

Don't worry about deleting these files!

********* C O N F I G U R A T I O N **************************************
Drakeville will look for a file called IGM.DAT. The file may exist or not,
so your IGM should check if it exists or not before trying to install.
If the file already exists just add your 2 lines needed to the end of the
file. The lines should be like this.

Line 1 : The command to start the IGM (ex. pub *) The '*' will be replaced
with the node number by Drakeville. Do not put a number yourself.
Line 2 : The description (ex. ^2Da ^APUB!)

Make sure that your IGM is solid. Make sure that you have a program that can
install it and uninstall it. ;-O
}

