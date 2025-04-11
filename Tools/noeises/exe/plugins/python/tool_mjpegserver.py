import noewin
from noewin import user32, gdi32, kernel32
from inc_noesis import *
from ctypes import *
from urllib.parse import unquote
import http.server
import socketserver
import ssl
import select
import threading
import gc
import time
import posixpath
import urllib.parse
import json
import io
import collections

#this is mostly copy-pasted from the old tool_modelserver script, slightly adapted to shoehorn in mjpeg streaming

MJPEG_STREAM_PATH = "/noestream.mjpeg"
MJPEG_FRAME_PERIOD = 33.3333 / 1000.0
MJPEG_ENCODING_QUALITY = 70
MJPEG_ENCODING_SHIFT = 0

START_SERVER_ON_LAUNCH = True

MJPEGSERVER_PORT = 8080
MJPEGSERVER_SOCKET_TIMEOUT = 5.0
MJPEGSERVER_HTTPS = False

MJPEGSERVER_COPY_CHUNK_SIZE = 128 * 1024

MJPEGSERVER_UPDATE_TIMER_ID = 666
MJPEGSERVER_UPDATE_TIMER_INTERVAL = 100

def registerNoesisTypes():
    handle = noesis.registerTool("MJPEG Server", mjpegServerToolMethod, "Launch the MJPEG Server.")
    return 1
    
class ModelHttpServer(socketserver.ThreadingMixIn, socketserver.TCPServer):
    def get_request(self):
        conn, addr = self.socket.accept()
        conn.settimeout(MJPEGSERVER_SOCKET_TIMEOUT)
        return conn, addr
    
class ModelHttpServerRequestHandler(http.server.SimpleHTTPRequestHandler):
    def finish(self):
        super().finish()
    
    def do_GET(self):
        try:
            f, fLength = self.send_head()
            if f:
                readBytesToDest(self.wfile, f, fLength)
                f.close()
        except:
            pass

    def do_HEAD(self):
        try:
            f, fLength = self.send_head()
            if f:
                f.close()
        except:
            pass

    def send_head(self):
        noeWnd = self.server.noeWnd
        isStream = False
        if self.path.startswith("/pub/"):
            try:
                binPath = unquote(self.path.split("/pub", 1)[1]).lower()
                if binPath not in noeWnd.pubList:
                    noesis.doException("No pub entry.")
                ctype = self.guess_type(binPath)
                fullPath = noeWnd.pubList[binPath]
                f = open(fullPath, "rb")
                f.seek(0, os.SEEK_END)
                dataLength = f.tell()
                f.seek(0, os.SEEK_SET)
            except:
                self.send_error(404, "File not found")
                return None, 0
        elif self.path == MJPEG_STREAM_PATH:
            ctype = "multipart/x-mixed-replace;boundary=--noestream"
            isStream = True
        else: #don't care what else they're after, shove the index back in their face
            data = noeWnd.templateIndex.encode("UTF-8")
            dataLength = len(data)
            f = io.BytesIO(data)
            ctype = "text/html"

        self.send_response(200)
        self.send_header("Content-type", ctype)
        if isStream:
            self.end_headers()
            print("Starting MJPEG stream.")
            self.server.streamUsers += 1
            sendTime = time.time() - MJPEG_FRAME_PERIOD
            while not self.server.wantTerminate:
                while (time.time() - sendTime) < MJPEG_FRAME_PERIOD:
                    noesis.nativeYield() #wouldn't scale nicely, but for our purposes, let's avoid sucking too much cpu in the interpreter
                    time.sleep(0.001) #since we don't care too much about precision
                try:
                    newImg = noesis.desktopCapGetOldestBuffer()
                    if newImg is not None:
                        sendTime = time.time()
                        #we can clear it immediately since we have a copy of the data already in python land
                        if self.server.streamUsers <= 1:
                            #for the common (just me) path, keep it as fresh as possible
                            noesis.desktopCapClearOldestBuffer()
                        else:
                            #otherwise hang onto at least one buffer at all times if there's more than one stream user, lazy way to ensure other users end up grabbing the same data
                            noesis.desktopCapClearOldestBufferIfAvailable(2)
                        imgData = newImg[0]
                        self.wfile.write(b"--noestream")
                        self.send_header("Content-type", "image/jpeg")
                        self.send_header("Content-Length", len(imgData))
                        self.end_headers()
                        self.wfile.write(imgData)
                except Exception as e:
                    print("MJPEG streaming exception:", e)
                    break
            self.server.streamUsers -= 1
            print("Stopped MJPEG stream.")
            return None, 0
        else:
            self.send_header("Content-Length", dataLength)
            self.send_header("Last-Modified", self.date_time_string(0))
            self.end_headers()
        return f, dataLength
    
class ModelServerThread(threading.Thread):
    def setNoeWnd(self, noeWnd):
        self.noeWnd = noeWnd
    def run(self):
        noeWnd = self.noeWnd
        httpServer = noeWnd.httpServer
        httpServer.serve_forever()
    
def stopExistingServer(noeWnd):
    if noeWnd.httpServer:
        httpServer = noeWnd.httpServer
        httpServer.wantTerminate = True
        httpServer.shutdown()
        noeWnd.httpServer = None
        gc.collect()
        
def buttonStartMethod(noeWnd, controlId, wParam, lParam):
    startButton = noeWnd.getControlByIndex(noeWnd.startButtonIndex)

    if noeWnd.httpServer:
        stopExistingServer(noeWnd)
        startButton.setText("Start")
        updateStatus(noeWnd, "Server stopped.")
        return

    startButton.setText("Stop")
    httpServer = ModelHttpServer(("", MJPEGSERVER_PORT), ModelHttpServerRequestHandler)
    httpServer.wantTerminate = False
    httpServer.streamUsers = 0
    httpServer.noeWnd = noeWnd
    
    httpServer.socket.settimeout(MJPEGSERVER_SOCKET_TIMEOUT)
    if MJPEGSERVER_HTTPS:
        httpServer.socket = ssl.wrap_socket(httpServer.socket, certfile="your_cert_file.pem", server_side=True)    
    
    t = ModelServerThread()
    noeWnd.httpServer = httpServer
    t.setNoeWnd(noeWnd)
    t.start()
    updateStatus(noeWnd, "Server running.")

def getPubPath():
    return noesis.getScenesPath() + "modelserver\\pub\\"

def updateIndexTemplate(noeWnd):
    with open(noesis.getScenesPath() + "modelserver\\template_mj_index.txt", "r") as f:
        noeWnd.templateIndex = f.read()

#was originally doing some fancier mapping stuff, but scraped that out for now
def updatePubListing(noeWnd):
    noeWnd.pubList = {}
    pubPath = os.path.join(getPubPath(), "")
    for root, dirs, files in os.walk(pubPath):
        for fileName in files:
            fullPath = os.path.join(root, fileName)
            noeWnd.pubList[getLocalFilePath(pubPath, fullPath).lower()] = fullPath
        
def getLocalFilePath(basePath, path):
    return path[len(basePath) - 1:].replace("\\", "/")

def readBytesToDest(fDst, fSrc, size):
    while size > 0:
        copySize = min(size, MJPEGSERVER_COPY_CHUNK_SIZE)
        fDst.write(fSrc.read(copySize))
        size -= copySize

def updateStatus(noeWnd, msg):
    #if we updated the control directly on another python thread, it could hit in the middle of a message pump,
    #which can cause problems at the windows messaging level despite the fact that threads aren't running async.
    noeWnd.deferredStatus = True
    noeWnd.deferredMsg = msg
    
def statusUpdateTimer(noeWnd, controlIndex, message, wParam, lParam):
    if wParam == MJPEGSERVER_UPDATE_TIMER_ID:
        if noeWnd.deferredStatus:
            noeWnd.deferredStatus = False
            statusBox = noeWnd.getControlByIndex(noeWnd.statusIndex)
            statusBox.setText(noeWnd.deferredMsg)

def mjpegServerToolMethod(toolIndex):
    if not noesis.desktopCapStart(1, int(MJPEG_FRAME_PERIOD * 1000.0), MJPEG_ENCODING_QUALITY, MJPEG_ENCODING_SHIFT):
        print("Failed to start desktop capture.")
        return -1

    noeWnd = noewin.NoeUserWindow("MJPEG Server", "MJPEGServerWindowClass", 600, 140)
    noeWindowRect = noewin.getNoesisWindowRect()
    if noeWindowRect:
        windowMargin = 64
        noeWnd.x = noeWindowRect[0] + windowMargin
        noeWnd.y = noeWindowRect[1] + windowMargin
    if not noesis.getWindowHandle():
        #if invoked via ?runtool, we're our own entity
        noeWnd.becomeStandaloneWindow()

    if noeWnd.createWindow():
        noeWnd.httpServer = None
        noeWnd.isProcessing = False
        noeWnd.abortProcessing = False
        noeWnd.deferredStatus = False
        noeWnd.setFont("Arial", 14)

        updateIndexTemplate(noeWnd)
        updatePubListing(noeWnd)

        noeWnd.createStatic("Status:", 16, 16, 110, 20)
        noeWnd.statusIndex = noeWnd.createEditBox(16, 38, 562, 20, "", None, False, True)
        
        noeWnd.startButtonIndex = noeWnd.createButton("Start", 16 + 460 + 6, 76, 96, 24, buttonStartMethod)
    
        user32.SetTimer(noeWnd.hWnd, MJPEGSERVER_UPDATE_TIMER_ID, MJPEGSERVER_UPDATE_TIMER_INTERVAL, 0)
        noeWnd.addUserControlMessageCallback(-1, noewin.WM_TIMER, statusUpdateTimer)
    
        if START_SERVER_ON_LAUNCH:
            buttonStartMethod(noeWnd, 0, 0, 0)
    
        noeWnd.doModal()
        stopExistingServer(noeWnd)
        
    noesis.desktopCapEnd()
    
    return 0
