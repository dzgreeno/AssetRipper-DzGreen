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
	let loadToken = 0;
	const status = document.getElementById('assetBrowserPreviewStatus');

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

	function loadModel(path) {
		if (!path) return;
		const token = ++loadToken;
		if (status) status.textContent = 'Loading preview…';
		clearSceneContent();
		BABYLON.SceneLoader.Append('', path, scene, loadedScene => {
			if (token !== loadToken) return;
			loadedScene.animationGroups.forEach(group => animationEnabled ? group.start(true) : group.pause());
			fitCameraToScene();
			if (status) status.textContent = `Preview ready · ${scene.meshes.filter(mesh => mesh.getTotalVertices() > 0).length} render mesh(es)`;
		}, null, (_scene, message) => {
			if (token === loadToken && status) status.textContent = `Preview unavailable: ${message}`;
		});
	}

	window.assetRipperModelPreview = { load: loadModel };

	document.getElementById('toggleModelLighting')?.addEventListener('click', event => {
		lightingEnabled = !lightingEnabled;
		light.intensity = lightingEnabled ? 1.05 : 0.05;
		event.currentTarget.textContent = `Lighting: ${lightingEnabled ? 'on' : 'off'}`;
	});
	document.getElementById('resetModelCamera')?.addEventListener('click', () => {
		camera.alpha = defaults.alpha;
		camera.beta = defaults.beta;
		camera.radius = defaults.radius;
		camera.setTarget(BABYLON.Vector3.Zero());
		fitCameraToScene();
	});
	document.getElementById('toggleModelAnimation')?.addEventListener('click', event => {
		animationEnabled = !animationEnabled;
		scene.animationGroups.forEach(group => animationEnabled ? group.play(true) : group.pause());
		event.currentTarget.textContent = `Animation: ${animationEnabled ? 'on' : 'off'}`;
	});

	engine.runRenderLoop(() => {
		engine.resize();
		scene.render();
	});
	window.addEventListener('resize', () => engine.resize());
	loadModel(glbPath);
}
