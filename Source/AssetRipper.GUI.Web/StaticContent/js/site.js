// For enabling and disabling descriptions based on the selected option in a select element
document.addEventListener('DOMContentLoaded', function () {
	// Get all select elements on the page
	var selects = document.querySelectorAll('select');

	// Iterate through each select element
	selects.forEach(function (select) {
		// Add event listener to the select element to update the descriptions
		select.addEventListener('change', function () {
			for (let i = 0; i < select.options.length; i++) {
				var option = select.options[i];
				var descriptionId = option.getAttribute('option-description');
				var description = document.getElementById(descriptionId);
				if (description) {
					if (i == select.selectedIndex) {
						//Enable description
						description.classList.remove('disabled');
					}
					else {
						//Disable description
						description.classList.add('disabled');
					}
				}
			}
		});

		// Trigger initial update to display the description for the default selected option
		select.dispatchEvent(new Event('change'));
	});
});

// For loading dynamic content into pre elements
document.addEventListener("DOMContentLoaded", async () => {
	const preElements = document.querySelectorAll('pre[dynamic-text-content]');

	preElements.forEach(async (preElement) => {
		const url = preElement.getAttribute('dynamic-text-content');

		try {
			const response = await fetch(url);
			if (!response.ok) {
				throw new Error(`Network response was not ok: ${response.statusText}`);
			}
			const data = await response.text();
			preElement.textContent = data;
		} catch (error) {
			console.error('Error fetching the content:', error);
			preElement.textContent = `Failed to load content: ${error.message}`;
		}
	});
});

// Lightweight live status dock for import, export, and Auto-Fix messages.
async function refreshAssetRipperStatus() {
	const output = document.querySelector('[data-status-output]');
	if (!output) {
		return;
	}

	try {
		const response = await fetch('/Status/Recent', { cache: 'no-store' });
		if (!response.ok) {
			return;
		}
		const lines = await response.json();
		if (Array.isArray(lines) && lines.length > 0) {
			output.textContent = lines.slice(-24).join('\n');
			output.dataset.state = lines.some(line => line.includes('[Error]')) ? 'error' : lines.some(line => line.includes('[Warning]')) ? 'warning' : 'ready';
		}
	} catch (error) {
		console.debug('AssetRipper status is temporarily unavailable.', error);
	}
}

document.addEventListener('DOMContentLoaded', () => {
	refreshAssetRipperStatus();
	window.setInterval(refreshAssetRipperStatus, 1200);
});

// Copy the complete diagnostics stream exactly as captured by the local status logger.
document.addEventListener('DOMContentLoaded', () => {
	const copyButton = document.getElementById('assetRipperCopyFullLog');
	if (!copyButton) return;

	copyButton.addEventListener('click', async () => {
		const originalLabel = copyButton.textContent;
		copyButton.disabled = true;
		copyButton.textContent = 'Copying…';
		try {
			const response = await fetch('/Status/Full', { cache: 'no-store' });
			if (!response.ok) throw new Error(`Status response ${response.status}`);
			const text = await response.text();
			if (navigator.clipboard?.writeText) {
				await navigator.clipboard.writeText(text);
			} else {
				const fallback = document.createElement('textarea');
				fallback.value = text;
				fallback.setAttribute('readonly', '');
				fallback.style.position = 'fixed';
				fallback.style.opacity = '0';
				document.body.appendChild(fallback);
				fallback.select();
				if (!document.execCommand('copy')) throw new Error('Clipboard access was unavailable');
				fallback.remove();
			}
			copyButton.textContent = 'Copied full log';
		} catch (error) {
			console.error('Could not copy the full diagnostic log:', error);
			copyButton.textContent = 'Use Save log';
		} finally {
			window.setTimeout(() => {
				copyButton.disabled = false;
				copyButton.textContent = originalLabel || 'Copy full log';
			}, 1800);
		}
	});
});

// Fixed top-bar navigation controls.
document.addEventListener('DOMContentLoaded', () => {
	const back = document.getElementById('assetRipperNavigateBack');
	const forward = document.getElementById('assetRipperNavigateForward');
	if (!back || !forward) return;

	const updateNavigationState = () => {
		back.disabled = window.history.length <= 1;
		back.classList.toggle('navigation-button-disabled', back.disabled);
	};

	back.addEventListener('click', () => window.history.back());
	forward.addEventListener('click', () => window.history.forward());
	window.addEventListener('popstate', updateNavigationState);
	window.addEventListener('pageshow', updateNavigationState);
	updateNavigationState();
});
