import { useEffect, useRef, useState } from 'react'

interface PacsViewerModalProps {
  isOpen: boolean
  onClose: () => void
  patientName: string
  patientMrn: string
  procedureName: string
  modality: string
  accessionNumber: string
  imageUrl?: string
}

interface Point {
  x: number
  y: number
}

interface CaliperLine {
  start: Point
  end: Point
  distanceMm: number
}

export function PacsViewerModal({
  isOpen,
  onClose,
  patientName,
  patientMrn,
  procedureName,
  modality,
  accessionNumber,
  imageUrl,
}: PacsViewerModalProps) {
  const canvasRef = useRef<HTMLCanvasElement | null>(null)
  const [image, setImage] = useState<HTMLImageElement | null>(null)
  const [imageLoading, setImageLoading] = useState(true)
  const [imageError, setImageError] = useState(false)
  const [seriesIndex, setSeriesIndex] = useState(0)

  // Interactive Viewport State
  const [scale, setScale] = useState(1.0)
  const [offset, setOffset] = useState<Point>({ x: 0, y: 0 })
  const [brightness, setBrightness] = useState(100)
  const [contrast, setContrast] = useState(100)
  
  // Caliper & Pan state
  const [tool, setTool] = useState<'pan' | 'caliper'>('pan')
  const [isDragging, setIsDragging] = useState(false)
  const [dragStart, setDragStart] = useState<Point>({ x: 0, y: 0 })
  const [caliperStart, setCaliperStart] = useState<Point | null>(null)
  const [currentMousePos, setCurrentMousePos] = useState<Point | null>(null)
  const [calipers, setCalipers] = useState<CaliperLine[]>([])

  // Resolve image source based on imageUrl prop or fallback
  const getImgSrc = () => {
    if (imageUrl) return imageUrl
    const mod = modality.toUpperCase()
    if (mod === 'XR') return '/images/chest_xray.png'
    if (mod === 'MRI' || mod === 'CT') return '/images/brain_mri.png'
    return '/images/abdominal_ultrasound.png'
  }

  const imgSrc = getImgSrc()

  // Load Image
  useEffect(() => {
    if (!isOpen) return
    setImageLoading(true)
    setImageError(false)
    const img = new Image()
    img.src = imgSrc
    img.onload = () => {
      setImage(img)
      setImageLoading(false)
      resetViewport(img)
    }
    img.onerror = () => {
      setImageLoading(false)
      setImageError(true)
    }
  }, [isOpen, imgSrc])

  // Reset Viewport
  const resetViewport = (currentImg?: HTMLImageElement | null) => {
    const targetImg = currentImg || image
    if (!targetImg || !canvasRef.current) return
    const canvas = canvasRef.current
    const rect = canvas.getBoundingClientRect()
    
    // Scale to fit
    const scaleX = (rect.width - 40) / targetImg.width
    const scaleY = (rect.height - 40) / targetImg.height
    const initialScale = Math.min(scaleX, scaleY, 1.0)
    
    setScale(initialScale)
    setOffset({
      x: (rect.width - targetImg.width * initialScale) / 2,
      y: (rect.height - targetImg.height * initialScale) / 2,
    })
    setBrightness(100)
    setContrast(100)
    setCalipers([])
    setCaliperStart(null)
  }

  // Draw Canvas
  useEffect(() => {
    if (!isOpen || !image || !canvasRef.current) return
    const canvas = canvasRef.current
    const ctx = canvas.getContext('2d')
    if (!ctx) return

    // Clear and Resize canvas to parent container client bounds
    const rect = canvas.parentNode ? (canvas.parentNode as HTMLElement).getBoundingClientRect() : { width: 600, height: 450 }
    canvas.width = rect.width
    canvas.height = rect.height

    ctx.fillStyle = '#09090b'
    ctx.fillRect(0, 0, canvas.width, canvas.height)

    // Set filters for contrast & brightness
    ctx.filter = `brightness(${brightness}%) contrast(${contrast}%)`

    // Draw Image
    ctx.drawImage(
      image,
      0, 0, image.width, image.height,
      offset.x, offset.y, image.width * scale, image.height * scale
    )

    // Reset filters for overlay text and caliper graphics
    ctx.filter = 'none'

    // Draw HUD text overlays
    ctx.fillStyle = '#71717a'
    ctx.font = 'bold 11px monospace'
    ctx.textBaseline = 'top'

    // Top Left: Patient info
    ctx.fillText(`PATIENT: ${patientName.toUpperCase()}`, 15, 15)
    ctx.fillText(`MRN: ${patientMrn}`, 15, 30)

    // Top Right: Study info
    ctx.textAlign = 'right'
    ctx.fillText(`ACCESSION: ${accessionNumber}`, canvas.width - 15, 15)
    ctx.fillText(`MODALITY: ${modality}`, canvas.width - 15, 30)
    ctx.fillText(`PROCEDURE: ${procedureName}`, canvas.width - 15, 45)

    // Bottom Left: Viewport scale info
    ctx.textAlign = 'left'
    ctx.textBaseline = 'bottom'
    ctx.fillText(`ZOOM: ${(scale * 100).toFixed(0)}%`, 15, canvas.height - 30)
    ctx.fillText(`WL: ${brightness} / WW: ${contrast}`, 15, canvas.height - 15)

    // Bottom Right: Series Info
    ctx.textAlign = 'right'
    ctx.fillText(`SERIES: ${seriesIndex + 1}/3`, canvas.width - 15, canvas.height - 30)
    ctx.fillText(`IMAGE: MOCK_DICOM_000${seriesIndex + 1}.DCM`, canvas.width - 15, canvas.height - 15)

    // Draw Saved Calipers
    calipers.forEach((c) => {
      drawCaliperLine(ctx, c.start, c.end, c.distanceMm.toFixed(1))
    })

    // Draw Active Drawing Caliper
    if (caliperStart && currentMousePos) {
      const distPx = Math.sqrt(
        Math.pow(currentMousePos.x - caliperStart.x, 2) +
        Math.pow(currentMousePos.y - caliperStart.y, 2)
      )
      // Simulated scale: 1px = 0.25mm
      const distMm = (distPx / scale) * 0.25
      drawCaliperLine(ctx, caliperStart, currentMousePos, distMm.toFixed(1))
    }
  }, [isOpen, image, scale, offset, brightness, contrast, calipers, caliperStart, currentMousePos, seriesIndex])

  const drawCaliperLine = (ctx: CanvasRenderingContext2D, p1: Point, p2: Point, labelMm: string) => {
    ctx.strokeStyle = '#22c55e'
    ctx.fillStyle = '#22c55e'
    ctx.lineWidth = 1.5

    // Line
    ctx.beginPath()
    ctx.moveTo(p1.x, p1.y)
    ctx.lineTo(p2.x, p2.y)
    ctx.stroke()

    // End-point tick marks
    drawTick(ctx, p1, p2)
    drawTick(ctx, p2, p1)

    // Label
    const midX = (p1.x + p2.x) / 2
    const midY = (p1.y + p2.y) / 2
    ctx.font = '11px sans-serif'
    ctx.textAlign = 'center'
    ctx.textBaseline = 'middle'
    
    // Small text background bubble
    const txt = `${labelMm} mm`
    const txtWidth = ctx.measureText(txt).width
    ctx.fillStyle = 'rgba(9, 9, 11, 0.75)'
    ctx.fillRect(midX - txtWidth / 2 - 4, midY - 7, txtWidth + 8, 14)
    
    ctx.fillStyle = '#22c55e'
    ctx.fillText(txt, midX, midY)
  }

  const drawTick = (ctx: CanvasRenderingContext2D, pStart: Point, pEnd: Point) => {
    const dx = pEnd.x - pStart.x
    const dy = pEnd.y - pStart.y
    const len = Math.sqrt(dx * dx + dy * dy)
    if (len === 0) return
    const ux = dx / len
    const uy = dy / len
    
    ctx.beginPath()
    ctx.moveTo(pStart.x - uy * 6, pStart.y + ux * 6)
    ctx.lineTo(pStart.x + uy * 6, pStart.y - ux * 6)
    ctx.stroke()
  }

  // Mouse Handlers
  const handleMouseDown = (e: React.MouseEvent<HTMLCanvasElement>) => {
    if (!canvasRef.current) return
    const rect = canvasRef.current.getBoundingClientRect()
    const x = e.clientX - rect.left
    const y = e.clientY - rect.top

    if (tool === 'caliper') {
      setCaliperStart({ x, y })
      setCurrentMousePos({ x, y })
    } else {
      setIsDragging(true)
      setDragStart({ x: e.clientX - offset.x, y: e.clientY - offset.y })
    }
  }

  const handleMouseMove = (e: React.MouseEvent<HTMLCanvasElement>) => {
    if (!canvasRef.current) return
    const rect = canvasRef.current.getBoundingClientRect()
    const x = e.clientX - rect.left
    const y = e.clientY - rect.top

    if (tool === 'caliper' && caliperStart) {
      setCurrentMousePos({ x, y })
    } else if (isDragging) {
      setOffset({
        x: e.clientX - dragStart.x,
        y: e.clientY - dragStart.y,
      })
    }
  }

  const handleMouseUp = () => {
    if (tool === 'caliper' && caliperStart && currentMousePos) {
      const dx = currentMousePos.x - caliperStart.x
      const dy = currentMousePos.y - caliperStart.y
      const distPx = Math.sqrt(dx * dx + dy * dy)
      if (distPx > 5) {
        const distanceMm = (distPx / scale) * 0.25
        setCalipers([...calipers, { start: caliperStart, end: { ...currentMousePos }, distanceMm }])
      }
      setCaliperStart(null)
      setCurrentMousePos(null)
    }
    setIsDragging(false)
  }

  const handleWheel = (e: React.WheelEvent<HTMLCanvasElement>) => {
    e.preventDefault()
    if (!canvasRef.current) return
    const rect = canvasRef.current.getBoundingClientRect()
    const x = e.clientX - rect.left
    const y = e.clientY - rect.top

    // Calculate zoom factor
    const zoomFactor = 1.1
    const nextScale = e.deltaY < 0 ? scale * zoomFactor : scale / zoomFactor
    
    // Cap zoom
    if (nextScale < 0.1 || nextScale > 10) return

    // Adjust offset to zoom relative to mouse pointer position
    const dx = x - offset.x
    const dy = y - offset.y
    
    setOffset({
      x: x - dx * (nextScale / scale),
      y: y - dy * (nextScale / scale),
    })
    setScale(nextScale)
  }

  if (!isOpen) return null

  return (
    <div className="fixed inset-0 z-50 bg-zinc-950 text-zinc-100 flex flex-col font-sans select-none">
      {/* Top Workspace Header */}
      <header className="h-14 bg-zinc-900 border-b border-zinc-800 flex items-center justify-between px-4 shrink-0">
        <div className="flex items-center gap-3">
          <span className="bg-sky-500/10 text-sky-400 border border-sky-500/20 px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider">
            PACS Workstation
          </span>
          <h2 className="text-sm font-semibold truncate max-w-xs sm:max-w-md">
            {patientName} · <span className="font-mono text-zinc-400">{patientMrn}</span>
          </h2>
        </div>
        
        {/* Workspace Operations Toolbar */}
        <div className="flex items-center gap-2">
          <button
            onClick={() => setTool('pan')}
            className={`px-3 py-1.5 rounded-lg text-xs font-semibold flex items-center gap-1.5 border transition-all ${
              tool === 'pan' 
                ? 'bg-zinc-800 border-zinc-700 text-white shadow-inner' 
                : 'bg-transparent border-transparent text-zinc-400 hover:text-white'
            }`}
          >
            ✋ Pan / Zoom
          </button>
          <button
            onClick={() => setTool('caliper')}
            className={`px-3 py-1.5 rounded-lg text-xs font-semibold flex items-center gap-1.5 border transition-all ${
              tool === 'caliper'
                ? 'bg-zinc-800 border-zinc-700 text-white shadow-inner'
                : 'bg-transparent border-transparent text-zinc-400 hover:text-white'
            }`}
          >
            📏 Caliper
          </button>
          <button
            onClick={() => resetViewport()}
            className="px-3 py-1.5 rounded-lg text-xs font-semibold text-zinc-400 hover:text-white hover:bg-zinc-850 transition-colors"
          >
            🔄 Reset
          </button>
          <span className="w-px h-5 bg-zinc-800 mx-2"></span>
          <button
            onClick={onClose}
            className="bg-red-950 hover:bg-red-900 text-red-200 border border-red-900/50 rounded-lg p-1.5 text-xs transition-colors"
            title="Exit workstation"
          >
            ✖ Close Viewer
          </button>
        </div>
      </header>

      {/* Main workspace section */}
      <div className="flex-1 flex overflow-hidden min-h-0">
        {/* Left Side: Series Navigator */}
        <aside className="w-48 bg-zinc-900 border-r border-zinc-800 flex flex-col shrink-0 overflow-y-auto">
          <div className="p-3 border-b border-zinc-850">
            <p className="text-[10px] font-bold text-zinc-500 uppercase tracking-widest">Series List</p>
          </div>
          <div className="p-2 space-y-2">
            {[0, 1, 2].map((idx) => (
              <button
                key={idx}
                onClick={() => setSeriesIndex(idx)}
                className={`w-full text-left p-2.5 rounded-xl border transition-all ${
                  seriesIndex === idx 
                    ? 'bg-zinc-800/80 border-sky-500/50 text-white shadow' 
                    : 'bg-zinc-950/20 border-zinc-850 text-zinc-400 hover:text-white'
                }`}
              >
                <div className="aspect-square bg-zinc-950 rounded-lg border border-zinc-850 mb-2 overflow-hidden flex items-center justify-center opacity-75">
                  {imageLoading ? (
                    <span className="text-[9px] text-zinc-600 animate-pulse">Loading…</span>
                  ) : (
                    <img src={imgSrc} className="w-full h-full object-cover filter brightness-75 grayscale" alt="Thumbnail" />
                  )}
                </div>
                <p className="text-[10px] font-bold truncate">Series {idx + 1}: {idx === 0 ? 'Scout' : idx === 1 ? 'Transverse' : 'Sagittal'}</p>
                <p className="text-[9px] text-zinc-500 font-mono mt-0.5">1 Slice · DICOM</p>
              </button>
            ))}
          </div>
        </aside>

        {/* Center: Interactive Viewer Area */}
        <main className="flex-1 relative bg-zinc-950 flex items-center justify-center overflow-hidden">
          {imageLoading && (
            <div className="absolute inset-0 z-10 bg-zinc-950/80 flex flex-col items-center justify-center gap-3">
              <div className="w-8 h-8 border-4 border-sky-500 border-t-transparent rounded-full animate-spin"></div>
              <p className="text-zinc-400 text-xs font-medium">Opening DICOM study...</p>
            </div>
          )}
          {imageError && (
            <div className="absolute inset-0 z-10 bg-zinc-950 flex flex-col items-center justify-center gap-3 p-4">
              <span className="text-4xl">⚠️</span>
              <p className="text-red-400 text-sm font-semibold">Failed to load medical scan asset.</p>
              <p className="text-zinc-500 text-xs text-center max-w-xs">Verify that image assets exist in the public directory `/images/`.</p>
            </div>
          )}

          <canvas
            ref={canvasRef}
            onMouseDown={handleMouseDown}
            onMouseMove={handleMouseMove}
            onMouseUp={handleMouseUp}
            onMouseLeave={handleMouseUp}
            onWheel={handleWheel}
            className={`w-full h-full ${tool === 'caliper' ? 'cursor-crosshair' : 'cursor-grab active:cursor-grabbing'}`}
          />
        </main>

        {/* Right Side: Image Processing Toolbar */}
        <aside className="w-56 bg-zinc-900 border-l border-zinc-800 p-4 space-y-5 shrink-0 overflow-y-auto">
          <div>
            <p className="text-[10px] font-bold text-zinc-500 uppercase tracking-widest mb-3">Adjustments</p>
            
            {/* Brightness slider */}
            <div className="space-y-1.5 mb-4">
              <div className="flex justify-between text-xs font-semibold text-zinc-300">
                <span>Brightness</span>
                <span className="font-mono text-zinc-400">{brightness}%</span>
              </div>
              <input
                type="range"
                min="50"
                max="200"
                value={brightness}
                onChange={(e) => setBrightness(Number(e.target.value))}
                className="w-full h-1 bg-zinc-800 rounded-lg appearance-none cursor-pointer accent-sky-500"
              />
            </div>

            {/* Contrast slider */}
            <div className="space-y-1.5">
              <div className="flex justify-between text-xs font-semibold text-zinc-300">
                <span>Contrast</span>
                <span className="font-mono text-zinc-400">{contrast}%</span>
              </div>
              <input
                type="range"
                min="50"
                max="200"
                value={contrast}
                onChange={(e) => setContrast(Number(e.target.value))}
                className="w-full h-1 bg-zinc-800 rounded-lg appearance-none cursor-pointer accent-sky-500"
              />
            </div>
          </div>

          <hr className="border-zinc-850" />

          {/* Measurements list */}
          <div className="flex-1 flex flex-col min-h-0">
            <p className="text-[10px] font-bold text-zinc-500 uppercase tracking-widest mb-3 flex items-center justify-between">
              <span>Measurements</span>
              {calipers.length > 0 && (
                <button
                  onClick={() => setCalipers([])}
                  className="text-[9px] text-red-400 hover:text-red-300 cursor-pointer font-bold lowercase"
                >
                  Clear All
                </button>
              )}
            </p>
            {calipers.length === 0 ? (
              <p className="text-zinc-500 text-xs py-4 text-center border border-dashed border-zinc-800 rounded-xl">
                No caliper lines drawn. Swap to the Caliper tool to draw ruler lines.
              </p>
            ) : (
              <div className="space-y-2 max-h-48 overflow-y-auto pr-1">
                {calipers.map((c, i) => (
                  <div key={i} className="bg-zinc-950/50 border border-zinc-850 rounded-xl p-2 flex items-center justify-between">
                    <div>
                      <p className="text-[10px] font-bold text-zinc-300">Measurement #{i + 1}</p>
                      <p className="text-[9px] text-zinc-500 font-mono mt-0.5">Scale: 0.25mm/px</p>
                    </div>
                    <span className="text-xs font-bold text-green-500 font-mono">
                      {c.distanceMm.toFixed(1)} mm
                    </span>
                  </div>
                ))}
              </div>
            )}
          </div>

          <hr className="border-zinc-850" />

          {/* Instructions Card */}
          <div className="bg-zinc-950/40 border border-zinc-850 rounded-xl p-3">
            <p className="text-[10px] font-bold text-zinc-400 uppercase tracking-wider mb-2">💡 Quick Help</p>
            <ul className="text-[10px] text-zinc-500 space-y-1 list-disc pl-3">
              <li>Use the scroll wheel to zoom the image relative to the cursor.</li>
              <li>Under Pan tool, drag the image to explore the scan.</li>
              <li>Under Caliper tool, click and drag on the image to measure lesions or structures.</li>
            </ul>
          </div>
        </aside>
      </div>

      {/* Bottom Status metadata bar */}
      <footer className="h-8 bg-zinc-900 border-t border-zinc-800 flex items-center justify-between px-4 text-[10px] text-zinc-500 shrink-0">
        <p>DICOM listener offline · Local simulation mode</p>
        <p className="font-mono text-zinc-400 uppercase">{modality} / {accessionNumber} · LOSSLESS COMPRESSION</p>
      </footer>
    </div>
  )
}
