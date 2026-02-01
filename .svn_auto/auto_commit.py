#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Auto SVN Commit Tool v3.2
- 自动解锁：先普通 cleanup，失败再强制清理
- 支持 --exclude
- 自动 add / rm / commit
- 支持自定义commit message和工作日志
"""
import argparse, subprocess, time, sys, json, io
from datetime import datetime
from pathlib import Path
from fnmatch import fnmatch

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

def run(cmd, cwd):
    try:
        p = subprocess.run(cmd, cwd=cwd, capture_output=True, text=True, encoding='utf-8', shell=False)
        return p.returncode, p.stdout.strip(), p.stderr.strip()
    except FileNotFoundError as e:
        return 127, "", str(e)

def parse_status(raw):
    res=[]
    for line in raw.splitlines():
        if line.strip():
            res.append((line[0], line[8:].strip()))
    return res

def filter_changes(changes, excludes):
    if not excludes: return changes
    return [(c,p) for c,p in changes if not any(fnmatch(p,pat) for pat in excludes)]

def stage(repo, changes, dry=False):
    adds=[p for c,p in changes if c=="?"]
    rms =[p for c,p in changes if c=="!"]
    if adds and not dry: subprocess.run(["svn","add","--parents"]+adds,cwd=repo)
    if rms  and not dry: subprocess.run(["svn","rm","--force"]+rms,cwd=repo)
    return any(c in "AMDR" for c,_ in changes)

def cleanup(repo):
    """两阶段 cleanup：先普通，再强制"""
    code,_,err=run(["svn","cleanup"],str(repo))
    if code==0:
        print("[清理] 普通 cleanup 成功")
        return True
    print("[警告] 普通 cleanup 失败，尝试强制清理：",err)
    code2,_,err2=run(["svn","cleanup","--remove-unversioned","--remove-ignored"],str(repo))
    if code2==0:
        print("[清理] 强制 cleanup 成功")
        return True
    print("[错误] cleanup 仍失败：",err2)
    return False

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--repo",default=".",help="SVN 工作副本路径")
    p.add_argument("--interval",type=int,default=300)
    p.add_argument("--once",action="store_true")
    p.add_argument("--dry-run",action="store_true")
    p.add_argument("--exclude",default="")
    p.add_argument("--message",default="",help="自定义commit message")
    p.add_argument("--message-file",default="",help="从文件读取commit message")
    p.add_argument("--log-history",default="",help="工作日志文件路径")
    a=p.parse_args()

    repo=Path(a.repo).resolve()
    if not repo.exists():
        print(f"[错误] 找不到仓库路径：{repo}")
        sys.exit(1)

    excludes=[x.strip() for x in a.exclude.split(",") if x.strip()]
    print(f"[启动] 监控路径：{repo}")
    if excludes: print(f"[过滤] 排除模式：{excludes}")

    def cycle():
        cleanup(repo)
        c,o,e=run(["svn","status","--ignore-externals"],str(repo))
        if c!=0:
            print("[错误] svn status 失败：",e)
            return
        ch=filter_changes(parse_status(o),excludes)
        if not ch:
            print("[空闲] 无变更")
            return
        stage(repo,ch,a.dry_run)
        if a.message_file:
            try:
                with open(a.message_file,"r",encoding="utf-8") as f:
                    msg=f.read().strip()
            except:
                msg=a.message if a.message else f"Auto commit | {datetime.now():%Y-%m-%d %H:%M:%S}"
        else:
            msg=a.message if a.message else f"Auto commit | {datetime.now():%Y-%m-%d %H:%M:%S}"
        if a.dry_run:
            print("[dry-run]",msg);return
        c2,o2,e2=run(["svn","commit","-m",msg],str(repo))
        if c2==0:
            print("[成功]",msg)
            if a.log_history:
                revision="unknown"
                for line in o2.splitlines():
                    if "Committed revision" in line:
                        revision=line.split()[-1].rstrip(".")
                        break
                log_entry={
                    "timestamp":datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
                    "message":msg,
                    "files":[p for c,p in ch if c in "AMDR"],
                    "svn_revision":revision
                }
                log_path=Path(a.log_history)
                with open(log_path,"a",encoding="utf-8") as f:
                    f.write(json.dumps(log_entry,ensure_ascii=False)+"\n")
        else: print("[失败]",e2)

    if a.once: cycle()
    else:
        while True:
            cycle()
            time.sleep(a.interval)

if __name__=="__main__":
    main()
