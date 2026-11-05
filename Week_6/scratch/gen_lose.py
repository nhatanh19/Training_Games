import hashlib, re, math
from PIL import Image, ImageDraw, ImageFilter
S=4
def lerp(a,b,t): return tuple(int(a[i]+(b[i]-a[i])*t) for i in range(3))
def face():
    W=320;H=280
    im=Image.new('RGBA',(W*S,H*S),(0,0,0,0))
    m=Image.new('L',(W*S,H*S),0); ImageDraw.Draw(m).ellipse([10*S,10*S,(W-10)*S,(H-10)*S],fill=255)
    g=Image.new('RGBA',(W*S,H*S))
    gd=ImageDraw.Draw(g)
    for y in range(H*S):
        t=y/(H*S); c=lerp((235,60,60),(255,170,40),min(1,max(0,(t-0.2)/0.75)))
        gd.line([(0,y),(W*S,y)],fill=c+(255,))
    im.paste(g,(0,0),m)
    d=ImageDraw.Draw(im)
    d.ellipse([55*S,28*S,150*S,78*S],fill=(255,255,255,90))
    # eyes
    for cx,sgn in ((105,1),(215,-1)):
        d.ellipse([(cx-26)*S,112*S,(cx+26)*S,160*S],fill=(255,255,255,255))
        d.ellipse([(cx-12)*S,124*S,(cx+12)*S,152*S],fill=(40,20,30,255))
        # angry brow
        pts=[((cx-34*sgn)*S,92*S),((cx+34*sgn)*S,112*S)]
        d.line(pts,fill=(60,20,30,255),width=14*S)
        for p in pts: d.ellipse([p[0]-7*S,p[1]-7*S,p[0]+7*S,p[1]+7*S],fill=(60,20,30,255))
    # frown
    pts=[(160+ (x*60)*1.0, 215-math.sqrt(max(0,1-x*x))*28) for x in [i/20 for i in range(-20,21)]]
    pts=[(x*S,y*S) for x,y in pts]
    d.line(pts,fill=(60,20,30,255),width=12*S,joint='curve')
    for p in (pts[0],pts[-1]): d.ellipse([p[0]-6*S,p[1]-6*S,p[0]+6*S,p[1]+6*S],fill=(60,20,30,255))
    return im.resize((W,H),Image.LANCZOS)
def btn():
    W,H=300,120
    im=Image.new('RGBA',(W*S,H*S),(0,0,0,0)); d=ImageDraw.Draw(im)
    d.rounded_rectangle([4*S,4*S,(W-4)*S,(H-4)*S],radius=34*S,fill=(255,214,120,255))
    d.rounded_rectangle([12*S,12*S,(W-12)*S,(H-12)*S],radius=28*S,fill=(46,130,16,255))
    d.rounded_rectangle([12*S,12*S,(W-12)*S,(H-26)*S],radius=28*S,fill=(112,214,36,255))
    d.rounded_rectangle([22*S,18*S,(W-22)*S,(H/2-6)*S],radius=20*S,fill=(160,240,80,255))
    return im.resize((W,H),Image.LANCZOS)
ui='Assets/Art/Sprites/UI/'
tm=open(ui+'ui_btn_cyan_pill.png.meta').read()
for n,im in (('lose_face_angry',face()),('lose_btn_green',btn())):
    im.save(ui+n+'.png')
    g=hashlib.md5(('week6lose'+n).encode()).hexdigest()
    open(ui+n+'.png.meta','w',newline='\n').write(re.sub(r'guid: [0-9a-f]{32}','guid: '+g,tm,count=1))
