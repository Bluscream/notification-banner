<?php // session_start();
ini_set('display_errors', 1);
ini_set('display_startup_errors', 1);
error_reporting(E_ALL);

function println($input = "") {
    echo("<br>".PHP_EOL.$input."<br>".PHP_EOL);
}
function get($input, $default = NULL) {
    if (is_array($input)) {
        foreach ($input as $key) {
            if (array_key_exists($key, $_GET) && !empty($_GET[$key])) {
                return $_GET[$key];
            }
        }
    } else {
        if (array_key_exists($input, $_GET) && !empty($_GET[$input])) {
            return $_GET[$input];
        }
    }
    return $default;
}
function has($key) {
    return isset($_GET[key]);
}
function getClientInfo($isForwarded = false) {
    $ipAddress = $isForwarded ? ($_SERVER['HTTP_X_FORWARDED_FOR'] ?? $_SERVER['REMOTE_ADDR']) : $_SERVER['REMOTE_ADDR'];
    $port = $_SERVER['REMOTE_PORT'] ?? '';
    $hostName = $_SERVER['HTTP_X_FORWARDED_HOST'] ?? $_SERVER['HTTP_X_FORWARDED_SERVER'] ?? '';
    if ($isForwarded && strpos($ipAddress, ',') !== false) {
        $ipAddress = explode(',', $ipAddress)[0];
    }
    return "$ipAddress:$port";
}
function exec2($command, $paramsArray) {
    $quotedParamsArray = array_map(function($param) {
        return preg_match('/\s/', $param) ? "\"$param\"" : $param;
    }, $paramsArray);
    $paramsString = implode(' ', $quotedParamsArray);
    $cmd = "$command $paramsString";
    exec($cmd, $output, $returnVar);
    return [$cmd, $output, $returnVar];
}

// if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $request_body = file_get_contents('php://input');
    if (!empty($request_body)) $_GET = json_decode($request_body);
    elseif (!empty($_POST)) $_GET = $_POST;
// }

$title = get("title", getClientInfo());
$message = get("message");
$image = get("image");

$args = [$message,$title];
if (!empty($image)) $args[] = $image;

$result = exec2("banner",$args);

var_dump($result);
