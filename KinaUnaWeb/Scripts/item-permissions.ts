import { ItemPermissionDto, PermissionLevel, TimelineItem, TimelineItemPermission, TimeLineType } from "./page-models-v9.js";

export async function renderItemPermissionsEditor(timelineItem: TimelineItem) {
    let url = '/AccessManagement/ItemPermissionsModal/';
    const permissionsDiv = document.getElementById('item-permissions-editor-div');
    if (permissionsDiv !== null) {
        permissionsDiv.innerHTML = '';
    }
    await fetch(url,
        {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(timelineItem),
        }).then(async (response) => {
            if (response.ok) {
                const data = await response.text();
                
                if (permissionsDiv !== null) {

                    permissionsDiv.innerHTML = data;
                    // Show the modal
                    permissionsDiv.classList.remove('d-none');
                    addPermissionTypeChangeListener();
                    setAdminPermissionsAsReadonly();
                    ($(".selectpicker") as any).selectpicker('refresh');
                }
            } else {
                console.error('Failed to load item permissions editor. Status:', response.status);
            }
        }).catch((error) => {
            console.error('Error in renderItemPermissionsEditor:', error);
        });
}

function addPermissionTypeChangeListener() {
    const permissionTypeSelect = document.getElementById('permission-type-select') as HTMLSelectElement;
    if (permissionTypeSelect !== null) {
        // On initial load, check if custom permissions should be shown.
        const customPermissionsDiv = document.getElementById('custom-permissions-div');
        if (customPermissionsDiv !== null && permissionTypeSelect.value === '3') {
            customPermissionsDiv.classList.remove('d-none');        
        }

        const inheritPermissionsDiv = document.getElementById('inherit-permissions-div');
        if (inheritPermissionsDiv !== null) {
            if (permissionTypeSelect.value !== '0') {
                inheritPermissionsDiv.classList.add('d-none');
            } else {
                inheritPermissionsDiv.classList.remove('d-none');
            }
        }
        const permissionTypeChanged =() => {
            const selectedValue = permissionTypeSelect.value;
            if (customPermissionsDiv !== null) {
                if (selectedValue === '3') {
                    customPermissionsDiv.classList.remove('d-none');
                } else {
                    customPermissionsDiv.classList.add('d-none');
                }
            }
            if (inheritPermissionsDiv !== null) {
                if (selectedValue === '0') {
                    inheritPermissionsDiv.classList.remove('d-none');
                } else {
                    inheritPermissionsDiv.classList.add('d-none');
                }
            }        
        }
        permissionTypeSelect.removeEventListener('change', permissionTypeChanged);
        permissionTypeSelect.addEventListener('change', permissionTypeChanged);
    }
}


function setAdminPermissionsAsReadonly() {
    // Admin permissions cannot be changed here, so disable the select input for them.
    const progenyPermissionsSelectInputs = document.querySelectorAll<HTMLSelectElement>('[data-progeny-permission-id]');
    const familyPermissionsSelectInputs = document.querySelectorAll<HTMLSelectElement>('[data-family-permission-id]');
    const itemPermissionsSelectInputs = document.querySelectorAll<HTMLSelectElement>('[data-item-permission-id]');

    progenyPermissionsSelectInputs.forEach((selectInput) => {
        const permissionId = selectInput.getAttribute('data-progeny-permission-id');
        if (permissionId !== null) {
            const selectedPermission = selectInput.value;
            if (selectedPermission === PermissionLevel.Admin.toString()) {
                selectInput.setAttribute('disabled', 'true');
            }
        }
    });
    familyPermissionsSelectInputs.forEach((selectInput) => {
        const permissionId = selectInput.getAttribute('data-family-permission-id');
        if (permissionId !== null) {
            const selectedPermission = selectInput.value;
            if (selectedPermission == PermissionLevel.Admin.toString()) {
                selectInput.setAttribute('disabled', 'true');
            }
        }
    });

    itemPermissionsSelectInputs.forEach((selectInput) => {
        const permissionId = selectInput.getAttribute('data-item-permission-id');
        if (permissionId !== null) {
            const selectedPermission = selectInput.value;
            if (selectedPermission == PermissionLevel.Admin.toString()) {
                selectInput.setAttribute('disabled', 'true');
            }
        }
    });
}

export function setPermissions(): void {
    const timelineItemPermissionsList: ItemPermissionDto[] = [];
    const permissionTypeSelect = document.getElementById('permission-type-select') as HTMLSelectElement;
    const permissionType = permissionTypeSelect.value;
    const inheritPermissionsInput = document.getElementById('inherit-permissions-input') as HTMLInputElement;
    const itemPermissionsListAsStringInput = document.getElementById('item-permissions-list-as-string-input') as HTMLInputElement;

    // Inherit permission type
    if (permissionType == '0') {
        if (inheritPermissionsInput !== null) {
            inheritPermissionsInput.value = 'true';
            let itemPermission = new ItemPermissionDto();
            itemPermission.inheritPermissions = true;
            itemPermission.permissionLevel = PermissionLevel.None;
            timelineItemPermissionsList.push(itemPermission);
        }
    }
    // Creator only permission type
    if (permissionType == '1') {
        if (inheritPermissionsInput !== null) {
            inheritPermissionsInput.value = 'false';
            let itemPermission = new ItemPermissionDto();
            itemPermission.inheritPermissions = false;
            itemPermission.permissionLevel = PermissionLevel.CreatorOnly;
            timelineItemPermissionsList.push(itemPermission);
        }
    }

    // Private permission type
    if (permissionType == '2') {
        if (inheritPermissionsInput !== null) {
            inheritPermissionsInput.value = 'false';
            let itemPermission = new ItemPermissionDto();
            itemPermission.inheritPermissions = false;
            itemPermission.permissionLevel = PermissionLevel.Private;
            timelineItemPermissionsList.push(itemPermission);
        }
    }

    // Custom permission type
    if (permissionType == '3') {
        const progenyPermissionsSelectInputs = document.querySelectorAll<HTMLSelectElement>('select[data-progeny-permission-id]');
        const familyPermissionsSelectInputs = document.querySelectorAll<HTMLSelectElement>('select[data-family-permission-id]');
        const itemPermissionsSelectInputs = document.querySelectorAll<HTMLSelectElement>('select[data-item-permission-id]');
        
        progenyPermissionsSelectInputs.forEach((selectInput) => {
            const permissionId = selectInput.getAttribute('data-progeny-permission-id');
            if (permissionId !== null) {
                const selectedPermission = selectInput.value;
                let itemPermission = new ItemPermissionDto();
                itemPermission.progenyPermissionId = parseInt(permissionId);
                itemPermission.permissionLevel = parseInt(selectedPermission);
                itemPermission.inheritPermissions = false;
                timelineItemPermissionsList.push(itemPermission);
            }
        });
        familyPermissionsSelectInputs.forEach((selectInput) => {
            const permissionId = selectInput.getAttribute('data-family-permission-id');
            if (permissionId !== null) {
                const selectedPermission = selectInput.value;
                let itemPermission = new ItemPermissionDto();
                itemPermission.familyPermissionId = parseInt(permissionId);
                itemPermission.permissionLevel = parseInt(selectedPermission);
                itemPermission.inheritPermissions = false;
                timelineItemPermissionsList.push(itemPermission);
            }
        });

        itemPermissionsSelectInputs.forEach((selectInput) => {
            const permissionId = selectInput.getAttribute('data-item-permission-id');
            if (permissionId !== null) {
                const selectedPermission = selectInput.value;
                let itemPermission = new ItemPermissionDto();
                itemPermission.itemPermissionId = parseInt(permissionId);
                itemPermission.permissionLevel = parseInt(selectedPermission);
                itemPermission.inheritPermissions = false;
                timelineItemPermissionsList.push(itemPermission);
            }
        });
    }

    itemPermissionsListAsStringInput.value = JSON.stringify(timelineItemPermissionsList);
}
