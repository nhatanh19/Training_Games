import math, hashlib, shutil
from PIL import Image, ImageDraw, ImageFilter
S=4
def rr_path(w,h,inset,r,step=1.0):
    x0,y0,x1,y1=inset,inset,w-inset,h-inset
    pts=[]
    def arc(cx,cy,a0,a1):
        n=max(4,int(r*abs(a1-a0)/step))
        for i in range(n+1):
            a=a0+(a1-a0)*i/n
            pts.append((cx+r*math.cos(a),cy+r*math.sin(a)))
    pts.append((x0+r,y0)); pts.append((x1-r,y0))
    arc(x1-r,y0+r,-math.pi/2,0); pts.append((x1,y1-r))
    arc(x1-r,y1-r,0,math.pi/2); pts.append((x0+r,y1))
    arc(x0+r,y1-r,math.pi/2,math.pi); pts.append((x0,y0+r))
    arc(x0+r,y0+r,math.pi,1.5*math.pi)
    return pts
def dashed(d,pts,dash,gap,width,col):
    acc=0;on=True;seg=dash
    for i in range(len(pts)-1):
        (ax,ay),(bx,by)=pts[i],pts[i+1]
        L=math.hypot(bx-ax,by-ay)
        if L==0: continue
        t=0
        while t<L:
            step=min(seg-acc,L-t)
            if on:
                p0=(ax+(bx-ax)*t/L,ay+(by-ay)*t/L); p1=(ax+(bx-ax)*(t+step)/L,ay+(by-ay)*(t+step)/L)
                d.line([p0,p1],fill=col,width=int(width))
                for p in (p0,p1): d.ellipse([p[0]-width/2,p[1]-width/2,p[0]+width/2,p[1]+width/2],fill=col)
            acc+=step;t+=step
            if acc>=seg-1e-6:
                acc=0;on=not on;seg=dash if on else gap
def slot(locked):
    W,H=320,640
    im=Image.new('RGBA',(W*S,H*S),(0,0,0,0)); d=ImageDraw.Draw(im)
    d.rounded_rectangle([14*S,14*S,(W-14)*S,(H-14)*S],radius=40*S,fill=(255,255,255,22))
    dashed(d,rr_path(W*S,H*S,14*S,40*S,2),30*S,20*S,8*S,(255,255,255,235))
    if locked:
        cx,cy,r=160*S,250*S,46*S
        d.ellipse([cx-r,cy-r,cx+r,cy+r],fill=(255,255,255,255))
        r2=r-6*S
        d.ellipse([cx-r2,cy-r2,cx+r2,cy+r2],fill=(52,168,98,255))
        k=24*S;t=8*S
        d.rounded_rectangle([cx-k,cy-t/2,cx+k,cy+t/2],radius=t/2,fill='white')
        d.rounded_rectangle([cx-t/2,cy-k,cx+t/2,cy+k],radius=t/2,fill='white')
    return im.resize((W,H),Image.LANCZOS)
def pause():
    W=256
    im=Image.new('RGBA',(W*S,W*S),(0,0,0,0)); d=ImageDraw.Draw(im)
    d.rounded_rectangle([8*S,8*S,(W-8)*S,(W-8)*S],radius=60*S,fill=(255,205,70,255))
    d.rounded_rectangle([22*S,22*S,(W-22)*S,(W-22)*S],radius=48*S,fill=(24,110,60,255))
    d.rounded_rectangle([30*S,30*S,(W-30)*S,(W-52)*S],radius=40*S,fill=(40,150,84,255))
    for x in (86,146):
        d.rounded_rectangle([x*S,72*S,(x+26)*S,184*S],radius=8*S,fill='white')
    return im.resize((W,W),Image.LANCZOS)
def plus():
    W=128
    im=Image.new('RGBA',(W*S,W*S),(0,0,0,0)); d=ImageDraw.Draw(im)
    d.ellipse([4*S,4*S,(W-4)*S,(W-4)*S],fill=(255,255,255,255))
    d.ellipse([12*S,12*S,(W-12)*S,(W-12)*S],fill=(52,176,96,255))
    d.ellipse([20*S,18*S,(W-20)*S,(W/2)*S],fill=(96,210,130,255))
    c=W/2*S;k=26*S;t=12*S
    d.rounded_rectangle([c-k,c-t/2,c+k,c+t/2],radius=t/2,fill='white')
    d.rounded_rectangle([c-t/2,c-k,c+t/2,c+k],radius=t/2,fill='white')
    return im.resize((W,W),Image.LANCZOS)
def pill():
    W,H=420,110
    im=Image.new('RGBA',(W*S,H*S),(0,0,0,0)); d=ImageDraw.Draw(im)
    d.rounded_rectangle([2*S,2*S,(W-2)*S,(H-2)*S],radius=55*S,fill=(40,44,56,150),outline=(255,255,255,40),width=2*S)
    return im.resize((W,H),Image.LANCZOS)
def guid(n): return hashlib.md5(('week6hud'+n).encode()).hexdigest()
tmpl=open('Assets/Art/Sprites/UI/ui_btn_cyan_pill.png.meta').read()
import re
def meta(path,n):
    open(path+'.meta','w',newline='\n').write(re.sub(r'guid: [0-9a-f]{32}','guid: '+guid(n),tmpl,count=1))
ui='Assets/Art/Sprites/UI/'
pause().save(ui+'hud_btn_pause.png'); meta(ui+'hud_btn_pause.png','pause')
plus().save(ui+'hud_btn_plus.png'); meta(ui+'hud_btn_plus.png','plus')
pill().save(ui+'hud_coin_pill.png'); meta(ui+'hud_coin_pill.png','pill')
for n,l in (('park_slot_unlock',False),('park_slot_lock',True)):
    im=slot(l)
    for p in ('Assets/Resources/Park/','Assets/Art/Sprites/Generated/Park/'):
        im.save(p+n+'.png')
print('ok')
