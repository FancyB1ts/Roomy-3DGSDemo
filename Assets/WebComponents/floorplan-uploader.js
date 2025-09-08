// Floorplan Uploader - Version with no session caching
// Floorplan Uploader - Version with no session caching
(function () {
    'use strict';

    // Canvas visibility helpers (Option B): keep Unity canvas laid out but hidden until Outliner activates
    function veilUnityContainer() {
        const el = document.getElementById('unity-container');
        if (!el) return;
        // Keep layout intact for Unity/Scaler; just hide visually
        el.style.opacity = '0';
        el.style.visibility = 'hidden';
        if (!el.style.transition) {
            el.style.transition = 'opacity 120ms ease';
        }
    }

    function revealUnityContainer() {
        const el = document.getElementById('unity-container');
        if (!el) return;
        // Give Outliner a frame or two to mount before reveal
        requestAnimationFrame(() => requestAnimationFrame(() => {
            el.style.visibility = 'visible';
            el.style.opacity = '1';
        }));
    }

    // ---- Stability + readiness helpers (minimal) ----
    function waitForStableRect(el, stableMs = 240, timeoutMs = 1800) {
        return new Promise((resolve) => {
            if (!el) return resolve();
            let last = el.getBoundingClientRect();
            let tStable = (performance?.now?.() || Date.now());
            const t0 = tStable;
            let rafId;
            const tick = () => {
                const r = el.getBoundingClientRect();
                const changed = r.width !== last.width || r.height !== last.height || r.top !== last.top || r.left !== last.left;
                const now = (performance?.now?.() || Date.now());
                if (changed) { last = r; tStable = now; }
                if (now - tStable >= stableMs || now - t0 >= timeoutMs) { if (rafId) cancelAnimationFrame(rafId); return resolve(); }
                rafId = requestAnimationFrame(tick);
            };
            rafId = requestAnimationFrame(tick);
        });
    }

    function waitUntil(pred, timeoutMs = 4000) {
        return new Promise((resolve) => {
            const t0 = (performance?.now?.() || Date.now());
            const loop = () => {
                if (pred() || ((performance?.now?.() || Date.now()) - t0) > timeoutMs) return resolve();
                requestAnimationFrame(loop);
            };
            loop();
        });
    }

    // Page scroll lock helpers
    function lockBodyScroll() {
        document.body.classList.add('lock-scroll');
    }
    function unlockBodyScroll() {
        document.body.classList.remove('lock-scroll');
    }

    // Constants
    const MAX_FILE_SIZE_MB = 5;
    const MAX_FILE_SIZE_BYTES = MAX_FILE_SIZE_MB * 1024 * 1024;

    // Unity Bridge
    const unityBridge = {
        // (Optional dedupe) Ignore identical re-sends within a short window.
        _lastPayload: null,
        _lastAt: 0,
        sendFloorplan: function (imageData) {
            if (!window.unityInstance) {
                throw new Error('Unity instance not available');
            }
            const now = Date.now();
            if (imageData === this._lastPayload && (now - this._lastAt) < 8000) {
                console.log('[Uploader] Duplicate payload ignored');
                return;
            }
            this._lastPayload = imageData; this._lastAt = now;
            const targets = [
                { go: 'FloorplanManager', method: 'ReceiveFloorplan' }
            ];
            let sent = false, lastErr;
            for (const t of targets) {
                try {
                    console.log(`[Uploader] Sending floorplan to ${t.go}.${t.method}`);
                    window.unityInstance.SendMessage(t.go, t.method, imageData);
                    console.log(`[Uploader] ✅ Sent via ${t.go}.${t.method}`);
                    sent = true; break;
                } catch (e) { lastErr = e; }
            }
            if (!sent) {
                console.error('[Uploader] ❌ Failed to send floorplan via known targets', lastErr);
            }
        },

        isAvailable: function () {
            return !!window.unityInstance;
        },

        setupCallbacks: function (onUnityReady, onFloorplanLoaded) {
            window.onUnityReady = function () {
                console.log("Unity is fully loaded and ready!");
                onUnityReady();
            };

            window.onFloorplanLoaded = function (status) {
                console.log("Unity floorplan processing: " + status);
                onFloorplanLoaded(status === 'success');
            };
        }
    };

    // Main Component
    function FloorplanUploader() {
        // Simple state: either 'hidden', 'uploader', 'processing', or 'unity'
        const [currentView, setCurrentView] = React.useState('uploader');
        const [unityLoaded, setUnityLoaded] = React.useState(false);
        const [uploadMode, setUploadMode] = React.useState('first-upload'); // 'first-upload' or 'replace-floorplan'
        const [hasEverLoadedFloorplan, setHasEverLoadedFloorplan] = React.useState(false);
        const [dragOver, setDragOver] = React.useState(false);
        const fileInputRef = React.useRef(null);
        const uploadModeRef = React.useRef('first-upload');
        const unityReadyRef = React.useRef(false);
        const receiverReadyRef = React.useRef(false);
        const sendInFlightRef = React.useRef(false);
        React.useEffect(function () { unityReadyRef.current = unityLoaded; }, [unityLoaded]);
        React.useEffect(function () { uploadModeRef.current = uploadMode; }, [uploadMode]);


        // Simplified: Always start on uploader, no localStorage, overlay/scroll logic
        React.useEffect(function () {
            // Always start on uploader for new sessions
            setCurrentView('uploader');
            setUploadMode('first-upload'); // Every new session starts as first upload
            
            // Apply initial overlay styling via body classes only
            if (window.Roomy && window.Roomy.setOverlayMode) {
                window.Roomy.setOverlayMode('first-upload');
            }
            document.body.classList.remove('show-unity');
            document.body.classList.remove('hide-react');
            lockBodyScroll();

            function handleUnityReady() {
                console.log("Unity ready callback received");
                setUnityLoaded(true);
            }

            function handleFloorplanLoaded(success) {
                if (success) {
                    console.log("Floorplan successfully loaded");
                    setHasEverLoadedFloorplan(true);

                    // Reveal Unity; hide React via body flags; remove overlay
                    setCurrentView('hidden');
                    if (window.Roomy && window.Roomy.setOverlayMode) {
                        window.Roomy.setOverlayMode('hidden');
                    }
                    document.body.classList.add('show-unity');
                    document.body.classList.add('hide-react');
                    document.body.classList.remove('is-overlay');
                    document.body.classList.remove('is-overlay--branded');
                    unlockBodyScroll();
                } else {
                    console.error("Failed to load floorplan");
                    if (window.Roomy && typeof window.Roomy.showErrorPopup === 'function') {
                        window.Roomy.showErrorPopup("We're having trouble loading your floorplan. Please try uploading again.");
                    }
                    // Return to uploader and keep overlay per current mode
                    document.body.classList.remove('hide-react');
                    setCurrentView('uploader');
                    lockBodyScroll();
                }
            }

            // Handle "Replace floorplan" button from Unity
            window.showFloorplanUploader = function () {
                console.log("Unity replace floorplan button clicked");

                // When invoked from Unity, we always want the dim veil overlay with a Cancel button
                const mode = 'replace-floorplan';
                uploadModeRef.current = mode;
                setUploadMode(mode);

                // Apply appropriate overlay styling (veil only, no branded background)
                if (window.Roomy && window.Roomy.setOverlayMode) {
                    window.Roomy.setOverlayMode('replace-floorplan');
                }

                // Keep Unity visible under veil; ensure React is shown
                document.body.classList.add('show-unity');
                document.body.classList.remove('hide-react');
                setCurrentView('uploader');
                lockBodyScroll();
            };

            unityBridge.setupCallbacks(handleUnityReady, handleFloorplanLoaded);
            
            // Other Unity callbacks...
            window.onReceiverReady = function () {
                console.log('[Unity] Receiver is ready');
                receiverReadyRef.current = true;
            };
            
            window.onOutlinerActivating = function () {
                revealUnityContainer();
            };
        }, []); // Only run once on mount

        // (showUnityContainer removed)

        // File upload - allow even while Unity loads
        function handleFileUpload(file) {
            if (!file || !file.type.startsWith('image/')) {
                return;
            }

            if (file.size > MAX_FILE_SIZE_BYTES) {
                if (window.Roomy && typeof window.Roomy.showErrorPopup === 'function') {
                    window.Roomy.showErrorPopup("Image too large. Please use an image under " + MAX_FILE_SIZE_MB + "MB.");
                }
                return;
            }



            const reader = new FileReader();
            reader.onload = function (e) {
                const imageData = e.target.result;
                sendImageToUnity(imageData);
            };
            reader.readAsDataURL(file);
        }

        async function sendImageToUnity(imageData) {
            try {
                console.log("Image selected, checking Unity readiness...");

                // Decide visibility based on mode to avoid flicker
                if (uploadModeRef.current === 'replace-floorplan') {
                    // Keep veil and show a small processing state so Unity's texture swap isn't visible
                    if (window.Roomy && window.Roomy.setOverlayMode) {
                        window.Roomy.setOverlayMode('replace-floorplan');
                    }
                    document.body.classList.add('show-unity');    // Unity stays visible under veil
                    document.body.classList.remove('hide-react'); // React overlay visible
                    setCurrentView('processing');                 // show spinner + Cancel
                    lockBodyScroll();
                } else {
                    // First upload: hide overlay and reveal Unity immediately
                    document.body.classList.add('show-unity');
                    document.body.classList.add('hide-react');
                    if (window.Roomy && window.Roomy.setOverlayMode) {
                        window.Roomy.setOverlayMode('hidden');
                    }
                    setCurrentView('hidden');
                    unlockBodyScroll();

                    // Nudge canvas sizing after mobile file picker closes (portrait devices)
                    setTimeout(() => window.Roomy && typeof Roomy.applyCanvasSize === 'function' && Roomy.applyCanvasSize(), 0);
                    setTimeout(() => window.Roomy && typeof Roomy.applyCanvasSize === 'function' && Roomy.applyCanvasSize(), 120);
                }

                const unityContainer = document.getElementById('unity-container');
                const canvas = document.getElementById('unity-canvas');

                if (sendInFlightRef.current) {
                    console.log('[Uploader] send already in-flight, ignoring');
                    return;
                }
                sendInFlightRef.current = true;

                // Wait until Unity (engine or receiver) is ready
                await waitUntil(() => (unityReadyRef.current || receiverReadyRef.current) && !!window.unityInstance, 45000);
                // Also wait for a short stable rect period (canvas or container)
                await waitForStableRect(canvas || unityContainer, 120, 800);

                console.log("[Uploader] Ready & stable – sending image to Unity");
                unityBridge.sendFloorplan(imageData);
            } catch (error) {
                console.error("Error sending image to Unity:", error);
                if (window.Roomy && typeof window.Roomy.showErrorPopup === 'function') {
                    window.Roomy.showErrorPopup("Oops! We’re having some trouble loading the floor planner. Please try uploading again.");
                }
                // Return to uploader
                if (window.Roomy && window.Roomy.setOverlayMode) {
                    window.Roomy.setOverlayMode('first-upload');
                }
                document.body.classList.remove('hide-react');
                setUploadMode('first-upload');
                setCurrentView('uploader');
                lockBodyScroll();
            } finally {
                sendInFlightRef.current = false;
            }
        }

        // Cancel function
        function handleCancel() {
            console.log("Upload cancelled by user");

            if (uploadMode === 'replace-floorplan') {
                // Go back to Unity; remove overlay; hide React
                setCurrentView('hidden');
                if (window.Roomy && window.Roomy.setOverlayMode) {
                    window.Roomy.setOverlayMode('hidden');
                }
                document.body.classList.add('show-unity');
                document.body.classList.add('hide-react');
                document.body.classList.remove('is-overlay');
                document.body.classList.remove('is-overlay--branded');
                unlockBodyScroll();
                revealUnityContainer();
            } else {
                // First-upload: stay on uploader; nothing to cancel
            }

            // Reset file input
            if (fileInputRef.current) {
                fileInputRef.current.value = '';
            }
        }

        function backToUploader() {
            if (uploadMode === 'replace-floorplan') {
                setCurrentView('hidden');
                setUploadMode('first-upload');
            } else {
                // Reload page since React container was removed
                window.location.reload();
            }
        }

        // Event handlers
        function handleDrop(e) {
            e.preventDefault();
            setDragOver(false);
            const files = Array.from(e.dataTransfer.files);
            if (files.length > 0) {
                handleFileUpload(files[0]);
            }
        }

        function handleDragOver(e) {
            e.preventDefault();
            setDragOver(true);
        }

        function handleDragLeave(e) {
            e.preventDefault();
            setDragOver(false);
        }

        function handleFileInputChange(e) {
            const file = e.target.files && e.target.files[0];
            if (file) {
                handleFileUpload(file);
            }
        }

        function triggerFileInput() {
            if (fileInputRef.current) {
                fileInputRef.current.click();
            }
        }

        // Don't render anything when hidden
        if (currentView === 'hidden') {
            return null;
        }

        // Processing screen
        if (currentView === 'processing') {
            const isOverlayMode = uploadMode === 'replace-floorplan';
            const containerClass = isOverlayMode ? 'overlay-container' : 'h-screen p-6 flex flex-col items-center justify-center';
            const containerStyle = isOverlayMode ? {} : {
                backgroundImage: "url('Background/left-bar.png'), url('Background/right-bar.png'), url('Background/center-bg.jpg')",
                backgroundPosition: "left center, right center, center center",
                backgroundRepeat: "no-repeat, no-repeat, no-repeat",
                backgroundSize: "auto 100%, auto 100%, cover",
                backgroundColor: "#ffffff"
            };
            const contentClass = isOverlayMode ? 'overlay-content' : 'bg-white rounded-xl shadow-lg p-12 text-center';

            return React.createElement('div', { 
                className: containerClass,
                style: containerStyle 
            },
                React.createElement('div', { className: contentClass },
                    React.createElement('div', { className: 'mb-6' },
                        React.createElement('div', {
                            className: 'w-12 h-12 mx-auto mb-4 border-4 border-blue-500 border-t-transparent rounded-full animate-spin'
                        }),
                        React.createElement('h2', {
                            className: 'text-2xl font-bold text-gray-900 mb-2'
                        }, 'Processing image...'),
                        React.createElement('p', {
                            className: 'text-gray-600'
                        }, unityLoaded ? 'Loading floorplan into Unity...' : 'Unity is loading, will process when ready...')
                    ),
                    React.createElement('button', {
                        onClick: handleCancel,
                        className: 'px-6 py-3 bg-gray-500 hover:bg-gray-600 text-white rounded-lg'
                    }, 'Cancel')
                )
            );
        }

        // Unity control panel (after React container is hidden)
        if (currentView === 'unity') {
            return null; // Unity is visible; no React overlay/panel
        }

        // Uploader (both initial and overlay modes)
        const isOverlayMode = uploadMode === 'replace-floorplan';
        const containerClass = isOverlayMode ? 'overlay-container' : 'h-screen p-6 flex flex-col';
        const containerStyle = isOverlayMode ? {} : {
            backgroundImage: "url('Background/left-bar.png'), url('Background/right-bar.png'), url('Background/center-bg.jpg')",
            backgroundPosition: "left center, right center, center center",
            backgroundRepeat: "no-repeat, no-repeat, no-repeat",
            backgroundSize: "auto 100%, auto 100%, cover",
            backgroundColor: "#ffffff"
        };
        const contentClass = isOverlayMode ? 'overlay-content' : 'max-w-4xl mx-auto flex-1 flex flex-col';
        const cardClass = isOverlayMode ? '' : 'bg-white rounded-xl shadow-lg p-8 flex-1 min-h-0 flex flex-col';

        return React.createElement('div', { 
            className: containerClass,
            style: containerStyle 
        },
            React.createElement('div', { className: contentClass },
                React.createElement('div', { className: cardClass },
                    // Unity status
                    React.createElement('div', { className: 'mb-4 text-center' },
                        React.createElement('div', {
                            className: 'inline-flex items-center space-x-2 px-4 py-2 rounded-lg text-sm ' +
                                (unityLoaded ? 'bg-green-100 text-green-800' : 'bg-blue-100 text-blue-800')
                        },
                            React.createElement('div', {
                                className: 'w-2 h-2 rounded-full ' +
                                    (unityLoaded ? 'bg-green-500' : 'bg-blue-500 animate-pulse')
                            }),
                            React.createElement('span', {}, unityLoaded ? 'Unity Ready' : 'Unity Loading...')
                        )
                    ),

                    // Upload area - always interactive
                    React.createElement('div', {
                        onDrop: handleDrop,
                        onDragOver: handleDragOver,
                        onDragLeave: handleDragLeave,
                        onClick: triggerFileInput,
                        className: 'upload-area border-3 border-dashed border-gray-300 bg-gray-50 rounded-xl p-12 text-center transition-all cursor-pointer mb-6' +
                            (dragOver ? ' drag-over' : '')
                    },
                        React.createElement('div', { className: 'mb-6' },
                            React.createElement('div', { className: 'file-icon' }, '📁'),
                            React.createElement('div', {
                                className: 'text-xl font-semibold text-gray-700 mb-2'
                            }, 'Drop your floorplan image here'),
                            React.createElement('div', {
                                className: 'text-gray-500'
                            }, 'or click to browse files')
                        ),
                        React.createElement('div', {
                            className: 'text-sm text-gray-400'
                        }, 'Max file size: ' + MAX_FILE_SIZE_MB + 'MB'),

                        React.createElement('input', {
                            ref: fileInputRef,
                            type: 'file',
                            accept: 'image/*',
                            onChange: handleFileInputChange,
                            className: 'hidden'
                        })
                    ),

                    // Cancel button (only in replace-floorplan mode)
                    uploadMode === 'replace-floorplan' ? React.createElement('div', { className: 'text-center' },
                        React.createElement('button', {
                            onClick: handleCancel,
                            className: 'px-6 py-3 bg-gray-500 hover:bg-gray-600 text-white rounded-lg transition-colors'
                        }, 'Cancel')
                    ) : null,

                    // Terms & Conditions notice (bottom of white card)
                    React.createElement('div', {
                        className: 'mt-auto pb-4 text-center',
                        style: { fontSize: '12px', color: '#9CA3AF' }
                    },
                        'By using this website, you agree to our ',
                        React.createElement('a', {
                            href: 'https://roomy-app.co/roomy-planner-terms-conditions/',
                            target: '_blank',
                            rel: 'noopener noreferrer',
                            style: { color: '#6B7280', textDecoration: 'underline' }
                        }, 'Terms & Conditions'),
                        '.'
                    )
                )
            )
        );
    }

    // App wrapper
    function App() {
        return React.createElement(FloorplanUploader);
    }
    // Expose as global for non-module usage (HTML checks or other bootstrap code)
    window.App = App;

    // Initialize
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            const container = document.getElementById('react-container');
            if (!container) {
                console.warn('React App container #react-container not found; skipping mount');
                return;
            }
            if (ReactDOM.createRoot) {
                const root = ReactDOM.createRoot(container);
                root.render(React.createElement(App));
            } else {
                // React 17 fallback
                ReactDOM.render(React.createElement(App), container);
            }
        });
    } else {
        const container = document.getElementById('react-container');
        if (!container) {
            console.warn('React App container #react-container not found; skipping mount');
            return;
        }
        if (ReactDOM.createRoot) {
            const root = ReactDOM.createRoot(container);
            root.render(React.createElement(App));
        } else {
            // React 17 fallback
            ReactDOM.render(React.createElement(App), container);
        }
    }
})();