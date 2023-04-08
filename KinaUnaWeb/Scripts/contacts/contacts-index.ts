
function hideItemsWithClass(classToHide: string) {
    const items = document.querySelectorAll('.' + classToHide);
    items.forEach((item) => {
        item.classList.add('d-none');
    });
}

function showItemsWithClass(classToShow: string) {
    const items = document.querySelectorAll('.' + classToShow);
    items.forEach((item) => {
        item.classList.remove('d-none');
    });
}

function updateFilterButtonDisplay(button: HTMLButtonElement) {
    const iconElement = button.querySelector<HTMLSpanElement>('.checkbox-icon');
    if (!button.classList.contains('active') && iconElement !== null) {
        iconElement.classList.value = '';
        iconElement.classList.add('checkbox-icon');
        iconElement.classList.add('fas');
        iconElement.classList.add('fa-check-square');
        button.classList.add('active');
        showItemsWithClass(button.name);
    }
    else {
        if (iconElement !== null) {
            iconElement.classList.value = '';
            iconElement.classList.add('checkbox-icon');
            iconElement.classList.add('fas');
            iconElement.classList.add('fa-square');
            button.classList.remove('active');
            hideItemsWithClass(button.name);
        }
    }
}

$(function () {

    const filterButtons = document.querySelectorAll('.button-checkbox');
    filterButtons.forEach((filterButtonParentSpan) => {
        
        let filterButton = filterButtonParentSpan.querySelector('button');
        if (filterButton !== null) {
            filterButton.addEventListener('click', function (this: HTMLButtonElement) {
                updateFilterButtonDisplay(this);
            });
        }
        
        if (filterButton !== null) {
            updateFilterButtonDisplay(filterButton);
        }
        
    });
});