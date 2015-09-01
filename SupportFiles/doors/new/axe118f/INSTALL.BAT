@echo off




cls
echo -=[ Building Directory Tree for Axe and Fang ]=-
md mobs
md objs
md zones
md world
md player
md data
md text
md docs
cd text
md boards
md notes
md rooms
md objs
md mobs
cd..
cd player
md a
md b
md c
md d
md e
md f
md g
md h
md i
md j
md k
md l
md m
md n
md o
md p
md q
md r
md s
md t
md u
md v
md w
md x
md y
md z
cd..
echo -=[ Directory Tree complete                  ]=-
echo -=[ Copying files to TEXT\*                  ]=-
copy room.mud text\rooms
copy mobs.mud text\mobs
copy objs.mud text\objs
echo -=[ Copying files to WORLD                   ]=-
copy world.mud world
echo -=[ Copying files to OBJS                    ]=-
copy obj.mud objs
echo -=[ Copying files to MOBS                    ]=-
copy mob.mud mobs
echo -=[ Copying files to DOCS                    ]=-
copy hyper.doc docs
copy axe.doc docs
copy axe.upd docs
echo -=[ Copying files to ZONES                   ]=-
copy zone.mud zones
del *.mud
del hyper.doc
del axe.doc
del axe.upd
echo -=[ Install complete                         ]=-
echo -=[ READ THE INSTALL DOC NOW!!!!!!!!!!!!!!!! ]=-
