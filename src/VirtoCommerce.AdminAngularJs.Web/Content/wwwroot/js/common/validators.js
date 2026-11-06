angular.module('platformWebApp').factory('platformWebApp.validators', [function () {
    function webSafeFileNameValidator(value) {
        var pattern = /^[\w.-]+$/;
        return pattern.test(value);
    }

    function uriWithoutQuery(value) {
        try {
            new URL(value);
            // url without query path and special chars
            var pattern = /[ !@#$%^&*()+=\[\]{}\\;'"|,<>?]/;
            return !pattern.test(value);
        } catch (_) {
            return false;
        }
    }

    function isLoopback(host) {
        return host === 'localhost' || host === '[::1]' || /^127(\.\d{1,3}){3}$/.test(host) || /\.localhost$/.test(host);
    }

    // Why a redirect URI of an OAuth application can't be used: 'invalid' (not absolute), 'script' (runs script in
    // the browser), 'fragment', or 'http' (HTTP is allowed only for loopback addresses, RFC 8252)
    function redirectUriProblem(value) {
        var url;
        try {
            url = new URL(value);
        } catch (_) {
            return 'invalid';
        }
        if (url.protocol === 'file:') {
            return 'invalid';
        }
        if (/^(javascript|vbscript|data):$/i.test(url.protocol)) {
            return 'script';
        }
        if (url.hash || value.indexOf('#') >= 0) {
            return 'fragment';
        }
        if (url.protocol === 'http:' && !isLoopback(url.hostname)) {
            return 'http';
        }
        return undefined;
    }

    function redirectUri(value) {
        return !redirectUriProblem(value);
    }

    function absoluteUri(value) {
        try {
            new URL(value);
            return true;
        } catch (_) {
            return false;
        }
    }

    function none(value) {
        return true;
    }

    return {
        webSafeFileNameValidator: webSafeFileNameValidator,
        uriWithoutQuery: uriWithoutQuery,
        redirectUri: redirectUri,
        redirectUriProblem: redirectUriProblem,
        absoluteUri: absoluteUri,
        none: none
    };
}]);
