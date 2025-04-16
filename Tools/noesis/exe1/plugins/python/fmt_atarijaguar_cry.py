from inc_noesis import *
from inc_atarijaguar import JagUtils

#default header produced by tga2cry doesn't have a distinction between cry and rgb (although it's implicit at >16bpp)
FORCE_RGB = False

def registerNoesisTypes():
    handle = noesis.register("Atari Jaguar CRY Image", ".cry")
    noesis.setHandlerTypeCheck(handle, cryCheckType)
    noesis.setHandlerLoadRGBA(handle, cryLoadRGBA)
    noesis.setHandlerWriteRGBA(handle, cryWriteRGBA)

    noesis.registerDebuggerDataHandler("Interpret CRY (headered)", lambda readAddr, resv: debuggerWrappedModuleInstance(readAddr, debuggerCheckCRYHeadered))
    noesis.registerDebuggerDataHandler("Interpret CRY (raw)", lambda readAddr, resv: debuggerWrappedModuleInstance(readAddr, debuggerCheckCRYRaw))
    return 1

class CryImage:    
    def __init__(self, data, validateWidth = True):
        self.pixelSize = 0
        if len(data) > 8:
            width, height, flags = noeUnpack(">HHI", data[:8])
            phraseGap, pixelSize, zOffset, decWidth = JagUtils.decodeBlitterFlags(flags)
            #only consider it valid if the encoded width matches the explicit one
            if (not validateWidth) or (width == decWidth):
                if pixelSize <= 32:
                    self.pixelDataSize = (width * height * pixelSize + 7) // 8
                    if len(data) >= (8 + self.pixelDataSize):
                        self.width = width
                        self.height = height
                        self.pixelSize = pixelSize
                        self.data = data

    def isValid(self):
        return self.pixelSize > 0
        
    def decode16(self, offset, colorCount):
        dataSlice = self.data[offset : offset + colorCount * 2]
        if FORCE_RGB:
            data = noesis.swapEndianArray(dataSlice, 2)
            return rapi.imageDecodeRaw(data, len(data) // 2, 1, "p1g5b5r5")
        else:
            return JagUtils.cryToRgba32(dataSlice)
        
    def decode(self):
        rgba = None
        if self.pixelSize >= 24:
            data = self.data[8 : 8 + self.width * self.height * 4]
            rgba = rapi.imageDecodeRaw(data, len(data) // 4, 1, "g8r8p8b8")
        elif self.pixelSize >= 16:
            rgba = self.decode16(8, self.width * self.height)
        else:
            bs = NoeBitStream(self.data, NOE_BIGENDIAN)
            bs.setByteEndianForBits(NOE_BIGENDIAN)
            bs.seek(8, NOESEEK_ABS)
            palOffset = 8 + self.pixelDataSize
            rgbaSize = self.width * self.height * 4
            rgba = bytearray(rgbaSize)
            if palOffset < len(self.data):
                bs.pushOffset()
                bs.seek(palOffset, NOESEEK_ABS)
                colorCount = bs.readUShort()
                bs.popOffset()
                
                palRgba = self.decode16(palOffset + 2, colorCount)
                for offset in range(0, rgbaSize, 4):
                    palEntryOffset = bs.readBits(self.pixelSize) * 4
                    rgba[offset : offset + 4] = palRgba[palEntryOffset : palEntryOffset + 4]
            else:
                for offset in range(0, rgbaSize, 4):
                    maskValue = bs.readBits(self.pixelSize)
                    rgba[offset : offset + 4] = (maskValue, maskValue, maskValue, 255)
        
        return NoeTexture("jagcrytex", self.width, self.height, rgba, noesis.NOESISTEX_RGBA32) if rgba else None                
    
def cryCheckType(data):
    try:
        cry = CryImage(data)
        return 1 if cry.isValid() else 0
    except:
        return 0

def cryLoadRGBA(data, texList):
    cry = CryImage(data)
    tex = cry.decode()
    if tex:
        texList.append(tex)
    return 1
    
def cryFlagsForWidth(width):
    #assume 0-phrase gap
    xAddInc = 0x30000
    pixel16 = (4 << 3)
    return JagUtils.closestTextureWidth(width) | xAddInc | pixel16

def cryWriteRGBA(data, width, height, bs):
    flags = cryFlagsForWidth(width)
    encWidth = JagUtils.decodeTextureWidth(flags)
    if encWidth != width:
        print("Warning: Unsupported width, resizing from", width, "to", encWidth)
        data = rapi.imageResample(data, width, height, encWidth, height)
    bs.setEndian(NOE_BIGENDIAN)
    bs.writeUShort(encWidth)
    bs.writeUShort(height)
    bs.writeUInt(flags)
    bs.writeBytes(JagUtils.rgba32ToCry(data[:encWidth * height * 4]))
    return 1

def debuggerDisplayIfCRY(data):
    if cryCheckType(data):
        dstName = noesis.getScenesPath() + "_cry_interpreted.cry"
        with open(dstName, "wb") as f:
            f.write(data)
        noesis.openAndRemoveTempFile(dstName)
        return 0
    return -1

def debuggerCheckCRYHeadered(readAddr):
    hdr = noesis.debuggerReadData(readAddr, 8)
    if not hdr:
        return -1
    width, height = noeUnpack(">HH", hdr[:4])
    dataSize = (width * height) << 1
    if dataSize <= 0 or dataSize > 0x2000000:
        return -1
    data = noesis.debuggerReadData(readAddr + 8, dataSize)
    if not data:
        return -1
        
    return debuggerDisplayIfCRY(hdr + data)

def debuggerCheckCRYRaw(readAddr):
    width = noesis.userPrompt(noesis.NOEUSERVAL_INT, "Width", "Enter width.", "16", 0)
    if not width:
        return 0
    height = noesis.userPrompt(noesis.NOEUSERVAL_INT, "Height", "Enter height.", "16", 0)
    if not height:
        return 0
    flags = cryFlagsForWidth(width)
    hdr = noePack(">HHI", width, height, flags)
    dataSize = (width * height) << 1
    data = noesis.debuggerReadData(readAddr, dataSize)
    if not data:
        return -1

    return debuggerDisplayIfCRY(hdr + data)

def debuggerWrappedModuleInstance(readAddr, checkProc):
    noeMod = noesis.instantiateModule()
    noesis.setModuleRAPI(noeMod)

    try:
        retVal = checkProc(readAddr)
    except:
        retVal = -1
        print("Exception while interpreting data as CRY.")
    
    noesis.freeModule(noeMod)
    
    return retVal
