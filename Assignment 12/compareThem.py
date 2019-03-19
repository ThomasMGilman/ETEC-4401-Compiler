import sys


def main():
    s1 = parse(sys.argv[1])
    s2 = parse(sys.argv[2])
    
    test( s1, 0, s2, 0, set() )
    test( s2, 0, s1, 0, set() )
    
    print("OK")
    
   
def test( dfa1, n1, dfa2, n2, visited ):
    visited.add(n1)
    
    qA = dfa1[n1]
    qB = dfa2[n2]
    
    if qA.items != qB.items:
        print(qA.items)
        print(qB.items)
        assert 0
        
    assert frozenset(qA.edgeDict.keys()) == frozenset(qB.edgeDict.keys())
    
    for sym in qA.edgeDict:
        nn1 = qA.edgeDict[sym]
        nn2 = qB.edgeDict[sym]
        if nn1 not in visited:
            test( dfa1, nn1, dfa2, nn2, visited )
        
    
class DFANode:
    def __init__(self,num,items):
        self.unique = num
        self.items = frozenset(items)
        self.edgeList = []
        self.edgeDict = {}
    def __str__(self):
        return str(self.unique)+"::"+str(self.items)+str(self.edgeDict)
    def __repr__(self):
        return str(self)

def parse(fname):
    states={}
    with open(fname) as fp:
        while 1:
            s = fp.readline().strip()
            if len(s) == 0:
                return states
            assert s.startswith("State ")
            tmp = s.split()
            stateNumber = int(tmp[1])
            s = fp.readline().strip()
            assert s.endswith(" items")
            tmp = s.split()
            numitems = int(tmp[0])
            items = []
            for i in range(numitems):
                s = fp.readline().strip()
                tmp = s.split()
                lhs = tmp[0]
                dpos = tmp[1]
                rhs = tuple(tmp[2:])
                items.append( (lhs,rhs,dpos) )
            Q = DFANode( stateNumber, items )
            assert stateNumber not in states
            states[stateNumber] = Q
            s = fp.readline().strip()
            assert s.endswith(" transitions")
            numtransitions = int(s.split()[0])
            for i in range(numtransitions):
                sym, st = fp.readline().strip().split()
                Q.edgeDict[sym] = int(st)
            

            

main()
