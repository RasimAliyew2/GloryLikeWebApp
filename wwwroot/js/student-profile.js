(function () {
    'use strict';

    const form = document.querySelector('[data-student-profile]');
    if (!form) return;

    const field = name => form.elements.namedItem('Input.' + name);
    const value = name => (field(name)?.value || '').trim();
    const safePhoto = photo => photo.length <= 2796230 && /^data:image\/(jpeg|png);base64,[A-Za-z0-9+/]*={0,2}$/.test(photo) ? photo : '';
    const save = form.querySelector('[data-profile-save]');
    const progress = form.querySelector('[data-profile-completion]');
    const completionLabel = form.querySelector('[data-profile-completion-label]');
    const photoInput = form.querySelector('[data-profile-photo-input]');
    const photoValue = form.querySelector('[data-profile-photo-value]');
    const photoImage = form.querySelector('[data-profile-photo-image]');
    const photoPlaceholder = form.querySelector('[data-profile-photo-placeholder]');
    const photoRemove = form.querySelector('[data-profile-photo-remove]');
    const photoError = form.querySelector('[data-profile-photo-error]');
    const preview = document.querySelector('[data-profile-preview]');
    const previewOpen = form.querySelector('[data-profile-preview-open]');
    let photoReadVersion = 0;
    let photoReading = false;

    function updateCompleteness() {
        if (!progress) return;
        const inputs = [...form.querySelectorAll('[data-profile-complete]')];
        const completed = inputs.filter(input => input.value.trim() && input.validity.valid).length;
        const percent = Math.round(completed * 100 / inputs.length);
        progress.setAttribute('aria-valuenow', String(percent));
        progress.querySelector('span').style.width = percent + '%';
        completionLabel.textContent = percent + '% complete';
    }

    function setPhoto(dataUrl) {
        photoValue.value = dataUrl;
        photoImage.hidden = !dataUrl;
        if (dataUrl) photoImage.src = dataUrl;
        else photoImage.removeAttribute('src');
        photoPlaceholder.hidden = !!dataUrl;
        photoRemove.hidden = !dataUrl;
    }

    function finishPhotoRead() {
        photoReading = false;
        if (save) save.disabled = false;
    }

    if (photoInput) {
        form.querySelector('[data-profile-photo-controls]').hidden = false;
        photoInput.addEventListener('change', async () => {
            const version = ++photoReadVersion;
            const file = photoInput.files?.[0];
            photoError.textContent = '';
            if (!file) { finishPhotoRead(); return; }
            if (!['image/jpeg', 'image/png'].includes(file.type)) {
                photoError.textContent = 'Choose a JPG or PNG image.';
                photoInput.value = '';
                finishPhotoRead();
                return;
            }
            if (file.size > 2 * 1024 * 1024) {
                photoError.textContent = 'Your photo must be 2 MB or smaller.';
                photoInput.value = '';
                finishPhotoRead();
                return;
            }
            photoReading = true;
            if (save) save.disabled = true;
            try {
                const signature = new Uint8Array(await file.slice(0, 8).arrayBuffer());
                if (version !== photoReadVersion) return;
                const isPng = [137, 80, 78, 71, 13, 10, 26, 10].every((byte, index) => signature[index] === byte);
                const isJpeg = signature[0] === 255 && signature[1] === 216 && signature[2] === 255;
                if ((file.type === 'image/png' && !isPng) || (file.type === 'image/jpeg' && !isJpeg)) throw new Error('invalid image');
                const reader = new FileReader();
                const dataUrl = await new Promise((resolve, reject) => {
                    reader.addEventListener('load', () => resolve(reader.result));
                    reader.addEventListener('error', reject);
                    reader.readAsDataURL(file);
                });
                if (version !== photoReadVersion) return;
                setPhoto(dataUrl);
            } catch {
                if (version !== photoReadVersion) return;
                photoError.textContent = "This image couldn't be loaded. Choose a valid JPG or PNG file.";
                photoInput.value = '';
            } finally {
                if (version === photoReadVersion) finishPhotoRead();
            }
        });
        photoRemove.addEventListener('click', () => {
            ++photoReadVersion;
            finishPhotoRead();
            photoInput.value = '';
            photoError.textContent = '';
            setPhoto('');
        });
    }

    form.addEventListener('input', updateCompleteness);
    form.addEventListener('change', updateCompleteness);
    form.addEventListener('submit', event => {
        if (photoReading) {
            event.preventDefault();
            photoError.textContent = 'Please wait for your photo to finish loading.';
            return;
        }
        if (save) {
            save.disabled = true;
            save.querySelector('span').textContent = 'Saving…';
        }
    });
    window.addEventListener('pageshow', () => {
        if (!save || !progress) return;
        save.disabled = photoReading;
        save.querySelector('span').textContent = 'Save';
    });

    if (preview && typeof preview.showModal === 'function') {
        previewOpen.hidden = false;
        const put = (name, text) => { preview.querySelector('[data-preview-' + name + ']').textContent = text; };
        previewOpen.addEventListener('click', () => {
            put('name', [value('FirstName'), value('LastName')].filter(Boolean).join(' ') || 'Your name');
            put('education', [value('University'), value('Specialty')].filter(Boolean).join(' · ') || 'Education not added yet');
            const study = [];
            if (value('StudyYear')) study.push('Year ' + value('StudyYear'));
            if (value('GraduationYear')) study.push('Graduating ' + value('GraduationYear'));
            put('study', study.join(' · '));
            put('about', value('About') || 'No bio added yet.');
            put('goal', value('Goal') || 'No internship goal added yet.');
            const open = form.querySelector('input[type="checkbox"][name="Input.OpenToInternships"]').checked;
            put('availability', open ? 'Open to internship opportunities' : 'Not currently open to internship opportunities');
            preview.querySelector('[data-preview-availability]').classList.toggle('closed', !open);
            const image = preview.querySelector('[data-preview-photo]');
            const photo = safePhoto(value('ProfileImageDataUrl'));
            image.hidden = !photo;
            if (photo) image.src = photo;
            else image.removeAttribute('src');
            preview.querySelector('[data-preview-photo-placeholder]').hidden = !!photo;
            preview.showModal();
        });
        preview.querySelectorAll('[data-profile-preview-close]').forEach(button => button.addEventListener('click', () => preview.close()));
        preview.addEventListener('close', () => previewOpen.focus());
        preview.addEventListener('click', event => {
            if (event.target !== preview) return;
            const bounds = preview.getBoundingClientRect();
            if (event.clientX < bounds.left || event.clientX > bounds.right || event.clientY < bounds.top || event.clientY > bounds.bottom) preview.close();
        });
    }
    updateCompleteness();
}());
