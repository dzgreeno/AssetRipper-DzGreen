(() => {
	const root = document.querySelector('[data-asset-browser="true"]');
	if (!root) return;

	const rows = Array.from(root.querySelectorAll('.asset-browser-row'));
	const search = root.querySelector('#assetBrowserSearch');
	const category = root.querySelector('#assetBrowserCategory');
	const classFilter = root.querySelector('#assetBrowserClass');
	const collection = root.querySelector('#assetBrowserCollection');
	const resultCount = root.querySelector('#assetBrowserResultCount');
	const table = root.querySelector('#assetBrowserTable');
	const workbench = root.querySelector('[data-asset-workbench="true"]');
	const workbenchGrid = root.querySelector('.asset-browser-workbench-grid');
	const hierarchyPanel = root.querySelector('.asset-browser-hierarchy');
	const inspectorPanel = root.querySelector('.asset-browser-workbench-inspector');
	const filesPanel = root.querySelector('#assetBrowserFilesPanel');
	const filesContent = root.querySelector('#assetBrowserFilesContent');
	const filesToggle = root.querySelector('#assetBrowserFilesToggle');
	const hierarchyToggle = root.querySelector('#assetBrowserHierarchyToggle');
	const inspectorToggle = root.querySelector('#assetBrowserInspectorToggle');
	const focusPreviewToggle = root.querySelector('#assetBrowserFocusPreview');
	const inspector = root.querySelector('#assetBrowserInspector');
	const inspectorEmpty = root.querySelector('#assetBrowserInspectorEmpty');
	const inspectorDetails = root.querySelector('#assetBrowserInspectorDetails');
	const inspectorName = root.querySelector('#assetBrowserInspectorName');
	const inspectorClass = root.querySelector('#assetBrowserInspectorClass');
	const inspectorCollection = root.querySelector('#assetBrowserInspectorCollection');
	const inspectorPathId = root.querySelector('#assetBrowserInspectorPathId');
	const inspectorComponents = root.querySelector('#assetBrowserInspectorComponents');
	const previewStatus = root.querySelector('#assetBrowserPreviewStatus');
	const workbenchTitle = root.querySelector('#assetBrowserWorkbenchTitle');
	const previewDownload = root.querySelector('#assetBrowserPreviewDownload');
	const contextLinks = {
		asset: root.querySelector('#assetBrowserContextAsset'),
		yaml: root.querySelector('#assetBrowserContextYaml'),
		json: root.querySelector('#assetBrowserContextJson'),
		dependencies: root.querySelector('#assetBrowserContextDependencies')
	};
	const actionLinks = {
		open: root.querySelector('#assetBrowserSelectedAssetOpen'),
		view: root.querySelector('#assetBrowserSelectedAssetView'),
		yaml: root.querySelector('#assetBrowserSelectedAssetYaml'),
		json: root.querySelector('#assetBrowserSelectedAssetJson'),
		model: root.querySelector('#assetBrowserSelectedAssetModel')
	};
	const modelCategories = new Set(['GameObject', 'Mesh', 'Material', 'Animation']);
	let selectedRow = null;
	let activeCharacter = root.querySelector('.asset-browser-character-choice');
	let filterFrame = 0;
	const storage = {
		get(key, fallback) {
			try {
				const value = window.localStorage.getItem(key);
				return value === null ? fallback : value;
			} catch (_) {
				return fallback;
			}
		},
		set(key, value) {
			try { window.localStorage.setItem(key, value); } catch (_) { /* private browsing */ }
		}
	};

	function normalized(value) {
		return String(value || '').normalize('NFKC').toLowerCase();
	}

	function matchesCategory(row, selectedCategory) {
		if (!selectedCategory) return true;
		if (selectedCategory === 'Model') return modelCategories.has(row.dataset.assetCategory || '');
		return row.dataset.assetCategory === selectedCategory;
	}

	function setHref(element, href) {
		if (!element) return;
		element.href = href || '#';
		element.hidden = !href;
	}

	function updateAssetLinks(data) {
		setHref(contextLinks.asset, data.viewUrl);
		setHref(contextLinks.yaml, data.yamlUrl);
		setHref(contextLinks.json, data.jsonUrl);
		setHref(contextLinks.dependencies, data.viewUrl);
		setHref(actionLinks.open, data.viewUrl);
		setHref(actionLinks.view, data.viewUrl);
		setHref(actionLinks.yaml, data.yamlUrl);
		setHref(actionLinks.json, data.jsonUrl);
		setHref(actionLinks.model, data.modelUrl);
	}

	function setPanelCollapsed(panel, button, collapsed, key, labels) {
		if (!panel || !button) return;
		panel.classList.toggle('is-collapsed', collapsed);
		button.setAttribute('aria-expanded', String(!collapsed));
		button.setAttribute('aria-pressed', String(collapsed));
		button.textContent = collapsed ? labels.show : labels.hide;
		storage.set(key, collapsed ? 'true' : 'false');
	}

	function bindPanelToggle(panel, button, key, labels, defaultCollapsed = false) {
		if (!panel || !button) return;
		const initial = storage.get(key, defaultCollapsed ? 'true' : 'false') === 'true';
		setPanelCollapsed(panel, button, initial, key, labels);
		button.addEventListener('click', () => setPanelCollapsed(panel, button, !panel.classList.contains('is-collapsed'), key, labels));
	}

	function setFocusPreview(active) {
		if (!workbench) return;
		workbench.classList.toggle('asset-browser-workbench-focus-preview', active);
		focusPreviewToggle?.setAttribute('aria-pressed', String(active));
		if (focusPreviewToggle) focusPreviewToggle.textContent = active ? 'Exit focus' : 'Focus preview';
		storage.set('assetripper.assetBrowser.focusPreview', active ? 'true' : 'false');
	}

	function updateInspector(data) {
		if (inspectorName) inspectorName.textContent = data.name || '-';
		if (inspectorClass) inspectorClass.textContent = `Class: ${data.className || '-'}`;
		if (inspectorCollection) inspectorCollection.textContent = `Collection: ${data.collection || '-'}`;
		if (inspectorPathId) inspectorPathId.textContent = `Path ID: ${data.pathId || '-'}`;
		if (inspectorComponents) inspectorComponents.textContent = `Components: ${data.components || data.className || '-'}`;
		if (inspectorEmpty) inspectorEmpty.hidden = true;
		if (inspectorDetails) inspectorDetails.hidden = false;
		inspector?.classList.add('asset-browser-inspector-active');
	}

	function selectCharacter(choice, loadPreview = true) {
		if (!choice) return;
		activeCharacter = choice;
		root.querySelectorAll('.asset-browser-character-choice').forEach(candidate => candidate.classList.toggle('active', candidate === choice));
		const name = choice.dataset.characterName || 'assembled character';
		const previewUrl = choice.dataset.characterPreviewUrl || '';
		const characterData = {
			name,
			className: 'GameObject',
			collection: choice.dataset.characterCollection || '-',
			pathId: choice.dataset.characterPathId || '-',
			components: choice.dataset.characterComponents || 'GameObject · Transform · resolved hierarchy',
			viewUrl: choice.dataset.characterAssetUrl || '',
			yamlUrl: choice.dataset.characterYamlUrl || '',
			jsonUrl: choice.dataset.characterJsonUrl || '',
			modelUrl: ''
		};
		if (workbenchTitle) workbenchTitle.textContent = `Assembled character · ${name}`;
		if (previewDownload) {
			previewDownload.href = previewUrl || '#';
			previewDownload.download = `${name}.glb`;
			previewDownload.hidden = !previewUrl;
		}
		updateInspector(characterData);
		updateAssetLinks(characterData);
		if (loadPreview && previewUrl && window.assetRipperModelPreview?.load) {
			if (previewStatus) previewStatus.textContent = `Loading assembled hierarchy · ${name}`;
			window.assetRipperModelPreview.load(previewUrl);
		}
	}

	function selectRow(row) {
		if (!row) return;
		selectedRow = row;
		rows.forEach(candidate => candidate.classList.toggle('asset-browser-row-selected', candidate === row));
		const data = {
			name: row.dataset.assetName || '-',
			className: row.dataset.assetClass || '-',
			collection: row.dataset.assetCollection || '-',
			pathId: row.querySelector('.asset-browser-path-id')?.textContent?.trim() || '-',
			components: row.dataset.assetComponents || row.dataset.assetClass || '-',
			viewUrl: row.dataset.assetViewUrl || row.querySelector('.asset-browser-name a')?.href || '',
			yamlUrl: row.dataset.assetYamlUrl || '',
			jsonUrl: row.dataset.assetJsonUrl || '',
			modelUrl: row.dataset.assetModelUrl || ''
		};
		updateInspector(data);
		updateAssetLinks(data);
		if (data.modelUrl && window.assetRipperModelPreview?.load) {
			if (workbenchTitle) workbenchTitle.textContent = `Selected asset · ${data.name}`;
			if (previewDownload) {
				previewDownload.href = data.modelUrl;
				previewDownload.download = `${data.name}.glb`;
				previewDownload.hidden = false;
			}
			if (previewStatus) previewStatus.textContent = `Loading mesh preview · ${data.name}`;
			window.assetRipperModelPreview.load(data.modelUrl);
		} else if (previewStatus) {
			previewStatus.textContent = `Selected ${data.className} · assembled character preview remains active`;
		}
	}

	function clearSelection() {
		selectedRow = null;
		rows.forEach(candidate => candidate.classList.remove('asset-browser-row-selected'));
		if (inspectorEmpty) inspectorEmpty.hidden = false;
		if (inspectorDetails) inspectorDetails.hidden = true;
		inspector?.classList.remove('asset-browser-inspector-active');
		if (previewStatus) previewStatus.textContent = 'No asset matches the current filters.';
	}

	function firstVisibleRow() {
		return rows.find(row => !row.hidden);
	}

	function applyFilters() {
		const query = normalized(search?.value);
		const selectedCategory = category?.value || '';
		const selectedClass = classFilter?.value || '';
		const selectedCollection = collection?.value || '';
		let visible = 0;
		rows.forEach(row => {
			const searchable = normalized([
				row.dataset.assetSearch,
				row.dataset.assetName,
				row.dataset.assetClass,
				row.dataset.assetCategory,
				row.dataset.assetCollection,
				row.dataset.assetComponents
			].join(' '));
			const matches = (!query || searchable.includes(query))
				&& matchesCategory(row, selectedCategory)
				&& (!selectedClass || row.dataset.assetClass === selectedClass)
				&& (!selectedCollection || row.dataset.assetCollection === selectedCollection);
			row.hidden = !matches;
			if (matches) visible++;
		});
		if (resultCount) resultCount.textContent = `${visible} of ${root.dataset.assetTotal || rows.length} assets`;
		if (selectedRow?.hidden) {
			const first = firstVisibleRow();
			if (first) selectRow(first); else clearSelection();
		}
	}

	function scheduleApplyFilters() {
		if (filterFrame) cancelAnimationFrame(filterFrame);
		filterFrame = requestAnimationFrame(() => {
			filterFrame = 0;
			applyFilters();
		});
	}

	function setQuickCategory(value) {
		if (category) category.value = value || '';
		root.querySelectorAll('.asset-browser-chip').forEach(chip => chip.classList.toggle('is-active', (chip.dataset.category || '') === value && value !== ''));
		root.querySelector('#assetBrowserQuickAll')?.classList.toggle('is-active', !value);
		applyFilters();
	}

	for (const control of [search, category, classFilter, collection]) {
		control?.addEventListener(control === search ? 'input' : 'change', scheduleApplyFilters);
	}
	rows.forEach(row => row.addEventListener('click', () => selectRow(row)));
	root.querySelector('#assetBrowserQuickAll')?.addEventListener('click', () => setQuickCategory(''));
	root.querySelectorAll('.asset-browser-chip[data-category]').forEach(chip => chip.addEventListener('click', () => setQuickCategory(chip.dataset.category || '')));
	root.querySelectorAll('.asset-browser-character-choice').forEach(choice => choice.addEventListener('click', () => selectCharacter(choice)));

	bindPanelToggle(filesPanel, filesToggle, 'assetripper.assetBrowser.filesCollapsed', { hide: 'Hide asset list', show: 'Show asset list' });
	bindPanelToggle(hierarchyPanel, hierarchyToggle, 'assetripper.assetBrowser.hierarchyCollapsed', { hide: 'Hierarchy', show: 'Show hierarchy' });
	bindPanelToggle(inspectorPanel, inspectorToggle, 'assetripper.assetBrowser.inspectorCollapsed', { hide: 'Asset actions', show: 'Show asset actions' });
	if (focusPreviewToggle) {
		const focusActive = storage.get('assetripper.assetBrowser.focusPreview', 'false') === 'true';
		setFocusPreview(focusActive);
		focusPreviewToggle.addEventListener('click', () => setFocusPreview(!workbench?.classList.contains('asset-browser-workbench-focus-preview')));
	}

	const listButton = root.querySelector('#assetBrowserListView');
	const gridButton = root.querySelector('#assetBrowserGridView');
	listButton?.addEventListener('click', () => {
		table?.classList.remove('asset-browser-grid');
		listButton.classList.add('btn-primary');
		listButton.classList.remove('btn-outline-secondary');
		gridButton?.classList.add('btn-outline-secondary');
		gridButton?.classList.remove('btn-primary');
	});
	gridButton?.addEventListener('click', () => {
		table?.classList.add('asset-browser-grid');
		gridButton.classList.add('btn-primary');
		gridButton.classList.remove('btn-outline-secondary');
		listButton?.classList.add('btn-outline-secondary');
		listButton?.classList.remove('btn-primary');
	});

	document.addEventListener('keydown', event => {
		if (event.key === '/' && document.activeElement !== search) {
			event.preventDefault();
			search?.focus();
		}
		if (event.key === 'Escape' && search?.value) {
			search.value = '';
			applyFilters();
		}
		if (event.key === 'Enter' && document.activeElement === search) {
			const first = firstVisibleRow();
			if (first) selectRow(first);
		}
	});

	if (inspectorDetails) inspectorDetails.hidden = true;
	if (activeCharacter) selectCharacter(activeCharacter, false);
	setQuickCategory('');
	selectRow(firstVisibleRow());
})();
