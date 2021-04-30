function confirmDelete(uniqueId, isDeleteClicked) {
    let deleteSpan = 'deleteSpan_' + uniqueId;
    let confirmDeleteSpan = 'confirmDeleteSpan_' + uniqueId;

    if (isDeleteClicked) {
        $('#' + deleteSpan).addClass("d-none");
        $('#' + confirmDeleteSpan).removeClass('d-none');
    } else {

        $('#' + deleteSpan).removeClass("d-none");
        $('#' + confirmDeleteSpan).addClass('d-none');

    }
}