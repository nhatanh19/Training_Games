import hashlib, re, math
from PIL import Image, ImageDraw
S=4
def lerp(a,b,t): return tuple(int(a[i]+(b[i]-a[i])*t) for i in range(3))

def face_happy():
    W=320;H=280
    im=Image.new('RGBA',(W*S,H*S),(0,0,0,0))
    m=Image.new('L',(W*S,H*S),0); ImageDraw.Draw(m).ellipse([10*S,10*S,(W-10)*S,(H-10)*S],fill=255)
    g=Image.new('RGBA',(W*S,H*S))
    gd=ImageDraw.Draw(g)
    for y in range(H*S):
        t=y/(H*S); c=lerp((255,225,55),(255,150,20),min(1,max(0,(t-0.1)/0.85)))
        gd.line([(0,y),(W*S,y)],fill=c+(255,))
    im.paste(g,(0,0),m)
    d=ImageDraw.Draw(im)
    # Glossy top highlight
    d.ellipse([55*S,24*S,155*S,74*S],fill=(255,255,255,115))
    # Rosy cheeks
    for cx in (72, 248):
        d.ellipse([(cx-24)*S,142*S,(cx+24)*S,170*S],fill=(255,95,85,130))
    # Happy star/curved eyes
    for cx in (108, 212):
        d.ellipse([(cx-24)*S,96*S,(cx+24)*S,144*S],fill=(55,25,18,255))
        d.ellipse([(cx-10)*S,104*S,(cx+4)*S,118*S],fill=(255,255,255,255))
    # Big joyful open smile
    d.pieslice([95*S,120*S,225*S,230*S],start=0,end=180,fill=(140,25,30,255),outline=(55,25,18,255),width=8*S)
    # Tongue inside mouth
    d.pieslice([125*S,178*S,195*S,226*S],start=0,end=180,fill=(255,95,110,255))
    # Crown / Star sparkles on top
    def star(sx,sy,r1,r2,col):
        pts=[]
        for i in range(10):
            a = -math.pi/2 + i*math.pi/5
            r = r1 if i%2==0 else r2
            pts.append(((sx+r*math.cos(a))*S, (sy+r*math.sin(a))*S))
        d.polygon(pts,fill=col,outline=(210,120,10,255))
    star(52, 55, 22, 10, (255,240,90,255))
    star(268, 55, 22, 10, (255,240,90,255))
    return im.resize((W,H),Image.LANCZOS)

ui='Assets/Art/Sprites/UI/'
tm=open(ui+'ui_btn_cyan_pill.png.meta').read()
im=face_happy()
n='win_face_happy'
im.save(ui+n+'.png')
g=hashlib.md5(('week6win'+n).encode()).hexdigest()
open(ui+n+'.png.meta','w',newline='\n').write(re.sub(r'guid: [0-9a-f]{32}','guid: '+g,tm,count=1))
