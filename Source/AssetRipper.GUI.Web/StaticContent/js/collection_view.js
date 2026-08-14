(() => {
	const search = document.getElementById('collectionAssetSearch');
	const table = document.getElementById('assetsTable');
	if (!search || !table) return;

	const rows = Array.from(table.querySelectorAll('tr'));
	const classFilter = document.getElementById('classFilter');
	const pageFilter = document.getElementById('assetsPerPage');
	const pageNumber = document.getElementById('pageNumber');
	const previousButton = document.getElementById('previousButton');
	const nextButton = document.getElementById('nextButton');
	let scheduled = false;

	function scheduleApply(resetPage) {
		if (resetPage && pageNumber) pageNumber.textContent = '1';
		if (scheduled) return;
		scheduled = true;
		window.setTimeout(() => {
			scheduled = false;
			applyAll();
		}, 0);
	}

	function applyAll() {
		const query = (search.value || '').trim().toLocaleLowerCase();
		const selectedClass = classFilter?.value || '';
		const matchingRows = rows.filter(row => {
			const matchesClass = !selectedClass || row.dataset.class === selectedClass;
			const matchesSearch = !query || (row.dataset.search || '').toLocaleLowerCase().includes(query);
			return matchesClass && matchesSearch;
		});
		const selectedPageSize = pageFilter?.value || '';
		let page = Math.max(1, Number.parseInt(pageNumber?.textContent || '1', 10) || 1);
		let pageSize = Number.parseInt(selectedPageSize, 10);
		const allRows = selectedPageSize === '' || !Number.isFinite(pageSize);
		const pageCount = allRows ? 1 : Math.max(1, Math.ceil(matchingRows.length / pageSize));
		page = Math.min(page, pageCount);
		if (pageNumber) pageNumber.textContent = String(page);

		const visibleSet = allRows
			? new Set(matchingRows)
			: new Set(matchingRows.slice((page - 1) * pageSize, page * pageSize));
		for (const row of rows) row.style.display = visibleSet.has(row) ? '' : 'none';
		if (previousButton) previousButton.disabled = page <= 1;
		if (nextButton) nextButton.disabled = allRows || page >= pageCount;
	}

	search.addEventListener('input', () => scheduleApply(true));
	classFilter?.addEventListener('change', () => scheduleApply(true));
	pageFilter?.addEventListener('change', () => scheduleApply(false));
	previousButton?.addEventListener('click', () => scheduleApply(false));
	nextButton?.addEventListener('click', () => scheduleApply(false));
	applyAll();
})();
