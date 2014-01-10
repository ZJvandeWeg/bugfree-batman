#!/usr/bin/env python
from ants import *
import random

class MyBot:
    def __init__(self):
        self.last_loc = None
    
    def do_setup(self, ants):
        pass
    
    def debug(self,str):
        if self.dodebug:
            self.f.write(str)
            self.f.flush()

    def do_turn(self, ants):
        free_ants = list(set(ants.my_ants())-set(ants.my_hills()))
        if len(free_ants)==0:
            walker = ants.my_ants()[0]
        else:
            walker = free_ants[0]

        directions = ['n','e','s','w']
        passable_directions = []
        for d in directions:
            new_loc = ants.destination(walker, d)
            if ants.passable(new_loc) and new_loc not in ants.my_hills() and new_loc != self.last_loc:
                passable_directions.append(d)

        if len(passable_directions)>0:
            dir = random.choice(passable_directions)
            ants.issue_order((walker,dir ))
        else:
            for d in ['n','e','s','w']:
                new_loc = ants.destination(walker, d)
                if new_loc==self.last_loc:
                    ants.issue_order((walker, d))

        self.last_loc = walker

            
if __name__ == '__main__':
    # psyco will speed up python a little, but is not needed
    try:
        import psyco
        psyco.full()
    except ImportError:
        pass
    
    try:
        Ants.run(MyBot())
    except KeyboardInterrupt:
        print('ctrl-c, leaving ...')
