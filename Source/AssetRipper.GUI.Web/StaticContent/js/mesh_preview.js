const canvas = document.getElementById('babylonRenderCanvas');

if (canvas && typeof BABYLON !== 'undefined') {
	const glbPath = canvas.getAttribute('glb-data-path') || '';
	const engine = new BABYLON.Engine(canvas, true, { preserveDrawingBuffer: true, stencil: true });
	const scene = new BABYLON.Scene(engine);
	const light = new BABYLON.HemisphericLight('assetRipperLight', new BABYLON.Vector3(0, 1, 0), scene);
	light.intensity = 1.05;
	const camera = new BABYLON.ArcRotateCamera('assetRipperCamera', Math.PI / 2, Math.PI / 2.4, 4, new BABYLON.Vector3(0, 0, 0), scene);
	camera.attachControl(canvas, true);

		const defaults = { alpha: camera.alpha, beta: camera.beta, radius: camera.radius };
		let lightingEnabled = true;
		let animationEnabled = true;
		let animationSpeed = 1;
		let autoRotationEnabled = false;
		let selectedAnimationTrack = '';
		let loadToken = 0;
		const status = document.getElementById('assetBrowserPreviewStatus');
		const backdropPresets = {
			atlas: { color: new BABYLON.Color4(0.078, 0.090, 0.078, 1), className: 'is-atlas' },
			studio: { color: new BABYLON.Color4(0.085, 0.105, 0.12, 1), className: 'is-studio' },
			light: { color: new BABYLON.Color4(0.79, 0.81, 0.77, 1), className: 'is-light' }
		};

	function clearSceneContent() {
		scene.animationGroups.slice().forEach(group => group.dispose());
		scene.meshes.slice().forEach(mesh => mesh.dispose(false, true));
	}

	function fitCameraToScene() {
		const meshes = scene.meshes.filter(mesh => mesh.isEnabled() && mesh.getTotalVertices() > 0);
		if (!meshes.length) return;
		const bounds = meshes.reduce((result, mesh) => {
			const info = mesh.getBoundingInfo().boundingBox;
			result.min = BABYLON.Vector3.Minimize(result.min, info.minimumWorld);
			result.max = BABYLON.Vector3.Maximize(result.max, info.maximumWorld);
			return result;
		}, { min: new BABYLON.Vector3(Number.POSITIVE_INFINITY, Number.POSITIVE_INFINITY, Number.POSITIVE_INFINITY), max: new BABYLON.Vector3(Number.NEGATIVE_INFINITY, Number.NEGATIVE_INFINITY, Number.NEGATIVE_INFINITY) });
		const center = bounds.min.add(bounds.max).scale(.5);
		const diagonal = bounds.max.subtract(bounds.min).length();
		camera.setTarget(center);
		camera.radius = Math.max(diagonal * 1.35, .5);
		defaults.alpha = camera.alpha;
		defaults.beta = camera.beta;
		defaults.radius = camera.radius;
	}

	function getAnimationTracks() {
		return scene.animationGroups.map(group => group.name);
	}

	function playSelectedAnimation() {
		const selected = scene.animationGroups.find(group => group.name === selectedAnimationTrack) || scene.animationGroups[0];
		if (!selected) return false;
		selectedAnimationTrack = selected.name;
		scene.animationGroups.forEach(group => group === selected ? group.stop() : group.stop());
			selected.speedRatio = animationSpeed;
			if (animationEnabled) selected.start(true); else selected.pause();
			return true;
		}

		function selectAnimationTrack(track) {
			selectedAnimationTrack = track || '';
			return playSelectedAnimation();
		}

		function frameModel() {
			camera.alpha = defaults.alpha;
			camera.beta = defaults.beta;
			fitCameraToScene();
		}

		function updateOrthographicBounds() {
			if (camera.mode !== BABYLON.Camera.ORTHOGRAPHIC_CAMERA) return;
			const aspect = Math.max(engine.getRenderWidth() / Math.max(engine.getRenderHeight(), 1), .1);
			const halfHeight = Math.max(camera.radius * .48, .5);
			camera.orthoLeft = -halfHeight * aspect;
			camera.orthoRight = halfHeight * aspect;
			camera.orthoTop = halfHeight;
			camera.orthoBottom = -halfHeight;
		}

		function setCameraDistance(percent) {
			const minimum = .35;
			const maximum = Math.max(defaults.radius * 3.6, 1);
			camera.radius = minimum + (maximum - minimum) * (Number(percent) / 100);
			updateRenderValue('assetBrowserCameraZoomValue', `${Math.round(Number(percent))}%`);
			updateOrthographicBounds();
		}

		function setLightingLevel(percent) {
			light.intensity = lightingEnabled ? Number(percent) / 100 : 0.03;
			updateRenderValue('assetBrowserLightingLevelValue', `${Math.round(Number(percent))}%`);
		}

		function setAnimationSpeed(percent) {
			animationSpeed = Math.max(Number(percent) / 100, .05);
			scene.animationGroups.forEach(group => { group.speedRatio = animationSpeed; });
			updateRenderValue('assetBrowserAnimationSpeedValue', `${Math.round(Number(percent))}%`);
		}

		function updateRenderValue(id, value) {
			const output = document.getElementById(id);
			if (output) output.textContent = value;
		}

		function setBackdrop(name) {
			const preset = backdropPresets[name] || backdropPresets.atlas;
			scene.clearColor = preset.color;
			canvas.parentElement?.classList.remove('is-atlas', 'is-studio', 'is-light');
			canvas.parentElement?.classList.add(preset.className);
		}

		function toggleProjection(button) {
			const orthographic = camera.mode !== BABYLON.Camera.ORTHOGRAPHIC_CAMERA;
			camera.mode = orthographic ? BABYLON.Camera.ORTHOGRAPHIC_CAMERA : BABYLON.Camera.PERSPECTIVE_CAMERA;
			updateOrthographicBounds();
			if (button) button.textContent = orthographic ? 'Orthographic' : 'Perspective';
		}

		function toggleAutoRotation(button) {
			autoRotationEnabled = !autoRotationEnabled;
			camera.useAutoRotationBehavior = autoRotationEnabled;
			if (camera.autoRotationBehavior) {
				camera.autoRotationBehavior.idleRotationSpeed = .28;
				camera.autoRotationBehavior.idleRotationWaitTime = 700;
				camera.autoRotationBehavior.idleRotationSpinupTime = 450;
			}
			if (button) {
				button.textContent = `Auto rotate: ${autoRotationEnabled ? 'on' : 'off'}`;
				button.setAttribute('aria-pressed', String(autoRotationEnabled));
			}
		}

		function capturePreview() {
			BABYLON.Tools.CreateScreenshotUsingRenderTarget(engine, camera, { width: 1920, height: 1080 }, data => {
				const download = document.createElement('a');
				download.href = data;
				download.download = 'AssetRipper-DzGreen-render.png';
				document.body.appendChild(download);
				download.click();
				download.remove();
				if (status) status.textContent = 'Render snapshot saved as PNG.';
			});
		}

	function loadModel(path) {
		if (!path) return;
		const token = ++loadToken;
		if (status) status.textContent = 'Loading preview…';
		clearSceneContent();
		BABYLON.SceneLoader.Append('', path, scene, loadedScene => {
			if (token !== loadToken) return;
				playSelectedAnimation();
				fitCameraToScene();
				updateOrthographicBounds();
				if (status) status.textContent = `Preview ready · ${scene.meshes.filter(mesh => mesh.getTotalVertices() > 0).length} render mesh(es)`;
		}, null, (_scene, message) => {
			if (token === loadToken && status) status.textContent = `Preview unavailable: ${message}`;
		});
	}

		window.assetRipperModelPreview = {
			load: loadModel,
			getAnimationTracks,
			selectAnimationTrack,
			frameModel,
			setCameraDistance,
			setLightingLevel,
			setAnimationSpeed,
			setBackdrop,
			toggleProjection,
			toggleAutoRotation,
			capturePreview
		};

		document.getElementById('toggleModelLighting')?.addEventListener('click', event => {
			lightingEnabled = !lightingEnabled;
			const control = document.getElementById('assetBrowserLightingLevel');
			setLightingLevel(control?.value || 105);
			event.currentTarget.textContent = `Lighting: ${lightingEnabled ? 'on' : 'off'}`;
		});
		document.getElementById('resetModelCamera')?.addEventListener('click', () => {
			frameModel();
		});
	document.getElementById('toggleModelAnimation')?.addEventListener('click', event => {
		animationEnabled = !animationEnabled;
		if (animationEnabled) playSelectedAnimation();
		else scene.animationGroups.find(group => group.name === selectedAnimationTrack)?.pause();
			event.currentTarget.textContent = `Animation: ${animationEnabled ? 'on' : 'off'}`;
		});
		document.getElementById('assetBrowserFrameModel')?.addEventListener('click', frameModel);
		document.getElementById('assetBrowserToggleProjection')?.addEventListener('click', event => toggleProjection(event.currentTarget));
		document.getElementById('assetBrowserToggleAutoRotate')?.addEventListener('click', event => toggleAutoRotation(event.currentTarget));
		document.getElementById('assetBrowserCapturePreview')?.addEventListener('click', capturePreview);
		document.getElementById('assetBrowserCameraZoom')?.addEventListener('input', event => setCameraDistance(event.currentTarget.value));
		document.getElementById('assetBrowserLightingLevel')?.addEventListener('input', event => setLightingLevel(event.currentTarget.value));
		document.getElementById('assetBrowserAnimationSpeed')?.addEventListener('input', event => setAnimationSpeed(event.currentTarget.value));
		document.getElementById('assetBrowserBackdrop')?.addEventListener('change', event => setBackdrop(event.currentTarget.value));
		setBackdrop('atlas');

	engine.runRenderLoop(() => {
		engine.resize();
		scene.render();
	});
		window.addEventListener('resize', () => { engine.resize(); updateOrthographicBounds(); });
	loadModel(glbPath);
}
