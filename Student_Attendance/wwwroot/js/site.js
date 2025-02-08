// Show modal popup
function showInPopup(url, title) {
    $.get(url, function (res) {
        $("#form-modal .modal-body").html(res);
        $("#form-modal .modal-title").html(title);
        $("#form-modal").modal('show');
    });
}

// Submit form using AJAX
function submitForm(form) {
    try {
        $.ajax({
            type: 'POST',
            url: form.action,
            data: new FormData(form),
            contentType: false,
            processData: false,
            success: function (res) {
                if (res.success) {
                    $("#form-modal").modal('hide');
                    Swal.fire({
                        title: 'Success!',
                        text: res.message,
                        icon: 'success',
                        confirmButtonText: 'OK'
                    }).then((result) => {
                        if (result.isConfirmed) {
                            location.reload();
                        }
                    });
                }
                else {
                    Swal.fire({
                        title: 'Error!',
                        text: res.message,
                        icon: 'error',
                        confirmButtonText: 'OK'
                    });
                }
            },
            error: function (err) {
                console.log(err);
            }
        });
    }
    catch (e) {
        console.log(e);
    }
    return false;
}

// Delete item
function deleteItem(url) {
    Swal.fire({
        title: 'Are you sure?',
        text: "You won't be able to revert this!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                type: 'POST',
                url: url,
                success: function (res) {
                    if (res.success) {
                        Swal.fire(
                            'Deleted!',
                            res.message,
                            'success'
                        ).then((result) => {
                            if (result.isConfirmed) {
                                location.reload();
                            }
                        });
                    }
                    else {
                        Swal.fire(
                            'Error!',
                            res.message,
                            'error'
                        );
                    }
                },
                error: function (err) {
                    console.log(err);
                }
            });
        }
    });
}

// Alert msg

function showAlert(message, type) {
    // Remove existing alerts
    $('.alert').remove();

    // Create alert HTML
    const alertHtml = `<div class="alert alert-${type} alert-dismissible fade show" role="alert">
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>`;

    // Insert alert before a suitable container (e.g., before the form)
    $('form').before(alertHtml);

    // Optional: Auto-dismiss after a few seconds
    setTimeout(function () {
        $('.alert').fadeOut('slow', function () {
            $(this).remove();
        });
    }, 5000);
}


