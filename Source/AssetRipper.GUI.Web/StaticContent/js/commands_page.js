const { createApp } = Vue

const app = createApp({
	data() {
		return {
			load_path: '',
			load_path_exists: false,
			export_path: '',
			export_path_has_files: false,
				create_subfolder: false,
				dialog_busy: false
		}
	},
	methods: {
		async handleLoadPathChange() {
			// Add a debounce mechanism to avoid too many requests in a short time
			if (this.debouncedInput) {
				clearTimeout(this.debouncedInput);
			}

			this.debouncedInput = setTimeout(async () => {
				try {
					this.load_path_exists = await this.fetchDirectoryExists(this.load_path) || await this.fetchFileExists(this.load_path);
				} catch (error) {
					console.error('Error fetching data:', error);
				}
			}, 300); // Adjust the debounce time as needed (300 milliseconds in this example)
		},
		async handleExportPathChange() {
			// Add a debounce mechanism to avoid too many requests in a short time
			if (this.debouncedInput) {
				clearTimeout(this.debouncedInput);
			}

			this.debouncedInput = setTimeout(async () => {
				try {
					if (this.create_subfolder) {
						this.export_path_has_files = false;
					} else {
						this.export_path_has_files = await this.fetchDirectoryExists(this.export_path) && !(await this.fetchDirectoryEmpty(this.export_path));
					}
				} catch (error) {
					console.error('Error fetching data:', error);
				}
			}, 300); // Adjust the debounce time as needed (300 milliseconds in this example)
		},
			async handleSelectLoadFile() {
				await this.openNativeDialog('/Dialogs/OpenFile', 'load_path', this.handleLoadPathChange);
			},
			async handleSelectLoadFolder() {
				await this.openNativeDialog('/Dialogs/OpenFolder', 'load_path', this.handleLoadPathChange);
			},
			async handleSelectExportFolder() {
				await this.openNativeDialog('/Dialogs/OpenFolder', 'export_path', this.handleExportPathChange);
			},
			async openNativeDialog(endpoint, target, refresh) {
				if (this.dialog_busy) {
					return;
				}
				this.dialog_busy = true;
				try {
					const response = await fetch(endpoint, { cache: 'no-store' });
					if (!response.ok) {
						throw new Error(`Native dialog request failed: ${response.status}`);
					}
					const selectedPath = await response.json();
					if (typeof selectedPath === 'string' && selectedPath.length > 0) {
						this[target] = selectedPath;
						await refresh.call(this);
					}
				} catch (error) {
					console.error('Native dialog error:', error);
				} finally {
					this.dialog_busy = false;
				}
			},
		async fetchFileExists(path) {
			const response = await fetch(`/IO/File/Exists?Path=${encodeURIComponent(path)}`);
			return await response.json();
		},
		async fetchDirectoryExists(path) {
			const response = await fetch(`/IO/Directory/Exists?Path=${encodeURIComponent(path)}`);
			return await response.json();
		},
		async fetchDirectoryEmpty(path) {
			const response = await fetch(`/IO/Directory/Empty?Path=${encodeURIComponent(path)}`);
			return await response.json();
		},
	}
})

const mountedApp = app.mount('#app')