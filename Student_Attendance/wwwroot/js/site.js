// Show modal popup
function showInPopup(url, title) {
    $.ajax({
        type: "GET",
        url: url,
        success: function (res) {
            $("#form-modal .modal-body").html(res);
            $("#form-modal .modal-title").html(title);
            $("#form-modal").modal('show');
        },
        error: function (err) {
            console.log(err);
        }
    });
}

// Submit form using AJAX
function submitForm(form) {
    var formData = new FormData(form);

    $.ajax({
        type: "POST",
        url: form.action,
        data: formData,
        contentType: false,
        processData: false,
        success: function (res) {
            console.log('Server response:', res);
            if (res && res.success === true) {  // Check explicitly for true
                showAlert("Data saved successfully", "success");
                $("#form-modal").modal('hide');
                setTimeout(function () {
                    location.reload();
                }, 1000);
            } else {
                showAlert(res.message || "Failed to save data", "danger");
            }
        },
        error: function (err) {
            console.error('Error:', err);
            showAlert("An error occurred while saving data", "danger");
        }
    });
    return false;
}



// Delete item
function deleteItem(url) {
    if (confirm('Are you sure you want to delete this item?')) {
        $.ajax({
            type: "POST",
            url: url,
            success: function (res) {
                location.reload();
            },
            error: function (err) {
                console.log(err);
                alert('An error occurred while deleting.');
            }
        });
    }
}

// Alert msg

function showAlert(message, type) {
    // Remove any existing alerts
    $('.alert').remove();

    // Create new alert
    var alertHtml = `<div class="alert alert-${type} alert-dismissible fade show" role="alert">
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>`;

    // Insert alert before the table
    $('.card-body').prepend(alertHtml);

    // Auto dismiss after 5 seconds
    setTimeout(function () {
        $('.alert').fadeOut('slow', function () {
            $(this).remove();
        });
    }, 5000);
}

function deleteItem(url) {
    if (confirm('Are you sure you want to delete this item?')) {
        $.ajax({
            type: "POST",
            url: url,
            success: function (res) {
                showAlert("Item deleted successfully", 'success');
                setTimeout(function () {
                    location.reload();
                }, 1000);
            },
            error: function (err) {
                console.log(err);
                showAlert('An error occurred while deleting.', 'danger');
            }
        });
    }
}


