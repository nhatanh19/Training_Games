import hashlib, re, math
from PIL import Image, ImageDraw
S=4
def panel():
    W,H=560,250
    im=Image.new('RGBA',(W*S,H*S),(0,0,0,0)); d=ImageDraw.Draw(im)
    d.rounded_rectangle([2*S,2*S,(W-2)*S,(H-2)*S],radius=30*S,fill=(120,190,165,255))
    d.rounded_rectangle([6*S,6*S,(W-6)*S,(H-6)*S],radius=27*S,fill=(184,226,208,255))
    # heart
    cx,cy=225*S,115*S
    def heart(cx,cy,sc,col):
        pts=[]
        for i in range(200):
            t=i/200*2*math.pi
            x=16*math.sin(t)**3
            y=13*math.cos(t)-5*math.cos(2*t)-2*math.cos(3*t)-math.cos(4*t)
            pts.append((cx+x*sc*S,cy-y*sc*S))
        d.polygon(pts,fill=col)
    heart(cx,cy+6*S,5.6,(190,20,30,255))
    heart(cx,cy,5.6,(236,40,48,255))
    d.ellipse([170*S,62*S,205*S,88*S],fill=(255,255,255,120))
    # plus
    px,py=292*S,176*S; k=22*S;t=11*S
    d.rounded_rectangle([px-k,py-t/2,px+k,py+t/2],radius=t/2,fill='white')
    d.rounded_rectangle([px-t/2,py-k,px+t/2,py+k],radius=t/2,fill='white')
    # slot ticket
    d.rounded_rectangle([335*S,70*S,405*S,170*S],radius=10*S,fill=(90,96,110,255),outline=(60,64,76,255),width=3*S)
    d.ellipse([352*S,95*S,388*S,131*S],fill=(210,214,222,255))
    d.rounded_rectangle([325*S,140*S,365*S,172*S],radius=6*S,fill=(250,190,40,255))
    d.arc([331*S,124*S,359*S,152*S],180,360,fill=(250,190,40,255),width=4*S)
    return im.resize((W,H),Image.LANCZOS)
def video():
    W=96
    im=Image.new('RGBA',(W*S,W*S),(0,0,0,0)); d=ImageDraw.Draw(im)
    d.rounded_rectangle([8*S,30*S,(W-8)*S,(W-10)*S],radius=8*S,fill=(215,40,48,255))
    d.polygon([(8*S,18*S),(28*S,8*S),(40*S,26*S),(14*S,34*S)],fill=(40,40,48,255))
    d.polygon([(40*S,12*S),(62*S,8*S),(72*S,26*S),(48*S,30*S)],fill=(250,250,250,255))
    d.polygon([(40*S,50*S),(40*S,76*S),(64*S,63*S)],fill='white')
    return im.resize((W,W),Image.LANCZOS)
ui='Assets/Art/Sprites/UI/'
tm=open(ui+'ui_btn_cyan_pill.png.meta').read()
for n,im in (('continue_reward_panel',panel()),('continue_icon_video',video())):
    im.save(ui+n+'.png')
    g=hashlib.md5(('week6cont'+n).encode()).hexdigest()
    open(ui+n+'.png.meta','w',newline='\n').write(re.sub(r'guid: [0-9a-f]{32}','guid: '+g,tm,count=1))
