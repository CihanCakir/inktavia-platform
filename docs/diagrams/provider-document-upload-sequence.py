import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import FancyBboxPatch, FancyArrowPatch, Rectangle

NAVY="#1b2a4a"; GOLD="#c9a227"; GREEN="#2f6b4d"; PURPLE="#6b4a80"; GREY="#5b6472"; RED="#a33"
actors=[("provider-web\n(browser)","#dde5f0",NAVY),
        ("MarineProvider\nBFF","#f7edcf","#4a3c0a"),
        ("Identity","#dff0e6",GREEN),
        ("FileStorage","#ece0f2",PURPLE),
        ("MinIO / S3","#1b2a4a","white")]
X=[1.0,3.6,6.2,8.8,11.4]
fig,ax=plt.subplots(figsize=(15.5,18.2)); ax.set_xlim(0,12.9); ax.set_ylim(0,20.2); ax.axis("off")

ax.text(6.45,19.85,"Inktavia Marine OS — Provider Document Upload: Signed-URL Flow",
        ha="center",fontsize=15,fontweight="bold",color=NAVY)
ax.text(6.45,19.52,"verified end-to-end in a real browser — PUT 200 · attach success · reload-persistent · signed read · delete",
        ha="center",fontsize=9.5,color=GREY)

for (name,fc,tc),x in zip(actors,X):
    ax.add_patch(FancyBboxPatch((x-0.82,18.62),1.64,0.62,boxstyle="round,pad=0.03,rounding_size=0.08",
                                fc=fc,ec="none"))
    ax.text(x,18.93,name,ha="center",va="center",fontsize=9.5,color=tc,fontweight="bold")
    ax.plot([x,x],[1.15,18.59],color="#c9d1dd",lw=1.1,ls=(0,(4,4)),zorder=0)

def arrow(y,a,b,label,color=GREY,style="-",lw=1.4,note=None,bold=False):
    x1,x2=X[a],X[b]
    ax.add_patch(FancyArrowPatch((x1,y),(x2,y),arrowstyle="-|>",mutation_scale=13,
        color=color,lw=lw,linestyle=style,shrinkA=2,shrinkB=2,zorder=3))
    mid=(x1+x2)/2
    ax.text(mid,y+0.11,label,ha="center",va="bottom",fontsize=8.6,color=color,
            fontweight="bold" if bold else "normal")
    if note:
        ax.text(mid,y-0.32,note,ha="center",va="top",fontsize=7.6,color=GREY,style="italic")

def step(n,y,text):
    ax.text(0.06,y,f"{n}",ha="left",va="center",fontsize=9,color="white",fontweight="bold",
            bbox=dict(boxstyle="circle,pad=0.28",fc=NAVY,ec="none"))
    ax.text(0.32,y,text,ha="left",va="center",fontsize=8.4,color=NAVY)

y=17.9
step(1,y,""); arrow(y,0,1,"POST /provider/files/upload-session",NAVY,bold=True,
      note="fileName · contentType · size  (declared by the client — not trusted)")
y-=0.78; arrow(y,1,1,"",GOLD)
ax.add_patch(FancyBboxPatch((X[1]-1.05,y-0.20),2.1,0.42,boxstyle="round,pad=0.02,rounding_size=0.06",fc="#f2e3b0",ec="none"))
ax.text(X[1],y,"ResolveAsync → UserId",ha="center",va="center",fontsize=8,color="#4a3c0a",fontweight="bold")
ax.text(X[1],y-0.32,"fail closed if UserId ≤ 0",ha="center",va="top",fontsize=7.4,color=RED,style="italic")

y-=0.85; arrow(y,1,3,"CreateUploadSession  (+ assertion headers)",PURPLE)
y-=0.60; arrow(y,3,4,"presign PUT · SigV4 · 15 min",PURPLE,style=(0,(3,2)))
y-=0.60; arrow(y,3,1,"fileId (Guid) · uploadUrl · requiredHeaders",PURPLE,style=(0,(3,2)))
y-=0.60; arrow(y,1,0,"200  { fileId, uploadUrl, requiredHeaders }",NAVY,style=(0,(3,2)),
      note="bucket / objectKey stripped — never sent to the SPA")

y-=1.0
step(2,y,""); arrow(y,0,4,"PUT  file bytes  →  presigned URL",NAVY,lw=2.6,bold=True,
      note="no Authorization header · Content-Type set exactly once (a duplicate breaks the signature)")
y-=0.72; arrow(y,4,0,"200",NAVY,style=(0,(3,2)))

y-=0.95
step(3,y,""); arrow(y,0,1,"POST /files/{fileId}/complete",NAVY,bold=True)
y-=0.62; arrow(y,1,3,"CompleteUploadSession",PURPLE)
y-=0.60; arrow(y,3,4,"HEAD object  (exists?)",PURPLE,style=(0,(3,2)),
      note="Phase 2h: real size + stored MIME + magic bytes — reject & delete on mismatch")
y-=0.78; arrow(y,1,0,"200  { status: Uploaded }",NAVY,style=(0,(3,2)))

y-=0.95
step(4,y,""); arrow(y,0,1,"POST /provider/onboarding/documents",NAVY,bold=True,
      note="fileId · documentType · issuer")
y-=0.75; arrow(y,1,2,"AttachProviderDocument  (asserted UserId)",GREEN)
y-=0.60; arrow(y,2,3,"GetFile: status · uploadedBy · type · size",GREEN,style=(0,(3,2)),
      note="fail closed — an unverifiable file is a rejection")
y-=0.78; arrow(y,2,3,"LinkToOwner  (claim)",GREEN)
y-=0.60; arrow(y,1,0,"200  { success: true, document }",NAVY,style=(0,(3,2)),
      note="success:false arrives inside a 200 envelope — the SPA checks the body, not just the envelope")

y-=1.0
step(5,y,""); arrow(y,0,1,"POST …/documents/{fileId}/access-url",NAVY,bold=True)
y-=0.68; arrow(y,1,2,"is this file attached to MY profile?",GREEN,style=(0,(3,2)),
      note="IDOR guard — minting a URL for any fileId would expose other providers' documents")
y-=0.78; arrow(y,1,3,"CreateReadUrl",PURPLE)
y-=0.58; arrow(y,3,4,"presign GET · SigV4 · 5 min",PURPLE,style=(0,(3,2)))
y-=0.58; arrow(y,1,0,"200  { url, expiresAt }",NAVY,style=(0,(3,2)),
      note="minted per click, used immediately, never stored")
y-=0.72; arrow(y,0,4,"GET signed URL  →  application/pdf",NAVY,lw=2.0)

ax.add_patch(Rectangle((0.05,0.18),12.8,0.62,fc="#fff7f7",ec="#e0b4b4",lw=1))
ax.text(0.25,0.49,"Known gap (Phase 2h):  deleting a document removes the Identity row, but the file stays claimed in FileStorage and the object stays in the bucket — "
                  "no unlink, no cleanup consumer.",
        ha="left",va="center",fontsize=8.4,color="#7a1f1f")

plt.tight_layout()
plt.savefig("/tmp/dg/seq.png",dpi=155,facecolor="white")
print("ok")
