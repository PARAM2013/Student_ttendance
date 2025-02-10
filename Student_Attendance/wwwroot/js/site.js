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

function loadStudents() {
    const divisionId = $('#DivisionId').val();
    const date = $('#Date').val();
    const subjectId = $('#SubjectId').val();

    if (!divisionId || !date || !subjectId) {
        showAlert('Warning', 'Please select all required fields', 'warning');
        return;
    }

    $.get('/Attendance/GetStudentsByDivision', {
        divisionId: divisionId,
        date: date,
        subjectId: subjectId
    })
    .done(function(data) {
        $('#studentList').html(data);
        $('#saveAttendance').show();
        initializeAttendanceHandlers();
    })
    .fail(function() {
        showAlert('Error', 'Failed to load students', 'error');
    });
}

function initializeAttendanceHandlers() {
    $('.attendance-check').on('change', function() {
        const reasonInput = $(this).closest('tr').find('.absence-reason');
        reasonInput.prop('disabled', $(this).is(':checked'));
        if ($(this).is(':checked')) {
            reasonInput.val('');
        }
    });
}

function showAlert(title, message, icon) {
    Swal.fire({
        title: title,
        text: message,
        icon: icon,
        confirmButtonText: 'OK'
    });
}

function markAttendance(form) {
    const data = {
        SubjectId: $('#SubjectId').val(),
        Date: $('#Date').val(),
        Students: []
    };

    $('.student-row').each(function() {
        const row = $(this);
        data.Students.push({
            StudentId: row.data('student-id'),
            IsPresent: row.find('.attendance-check').is(':checked'),
            AbsenceReason: row.find('.absence-reason').val()
        });
    });

    $.post('/Attendance/MarkAttendance', data, function(response) {
        if (response.success) {
            showAlert('Success', response.message, 'success');
        } else {
            showAlert('Error', response.message, 'error');
        }
    });

    return false;
}


